using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rota2.Data;
using Rota2.Models;

namespace Rota2.Services
{
    public class RotaService : IRotaService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        public RotaService(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public IEnumerable<Rota> GetAll()
        {
            return _db.Rotas
                .AsNoTracking()
                .Include(r => r.Shifts)
                .Include(r => r.RotaDoctors).ThenInclude(rd => rd.User)
                .Include(r => r.RotaAdmins).ThenInclude(ra => ra.User)
                .AsSplitQuery()
                .OrderBy(r => r.Id)
                .ToList();
        }

        public Rota? GetById(int id)
        {
            return _db.Rotas
                .Include(r => r.Shifts)
                .Include(r => r.RotaDoctors).ThenInclude(rd => rd.User)
                .Include(r => r.RotaAdmins).ThenInclude(ra => ra.User)
                .AsSplitQuery()
                .SingleOrDefault(r => r.Id == id);
        }

        public IEnumerable<Rota2.Models.ShiftAssignment> GetAssignments(int rotaId, DateTime start, DateTime end)
        {
            return _db.ShiftAssignments
                .AsNoTracking()
                .Include(sa => sa.User)
                .Include(sa => sa.Shift)
                .Where(sa => sa.RotaId == rotaId && sa.Date >= start.Date && sa.Date <= end.Date)
                .OrderBy(sa => sa.Date).ThenBy(sa => sa.ShiftId)
                .ToList();
        }

        public IEnumerable<Rota2.Models.ShiftAssignment> GetAssignmentsForUser(int userId, DateTime start, DateTime end)
        {
            return _db.ShiftAssignments
                .AsNoTracking()
                .Include(sa => sa.User)
                .Include(sa => sa.Shift)
                .Include(sa => sa.Rota)
                .Where(sa => sa.UserId == userId && sa.Date >= start.Date && sa.Date <= end.Date)
                .OrderBy(sa => sa.Date).ThenBy(sa => sa.ShiftId)
                .ToList();
        }

        public void CreateAssignments(int rotaId, DateTime start, DateTime end)
        {
            var rota = GetById(rotaId);
            if (rota == null) return;
            var doctorIds = rota.RotaDoctors.Select(rd => rd.UserId).ToList();

            // build list of slots (date + shift) within range matching shift day
            var slots = new List<(DateTime date, Shift shift)>();
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                foreach (var s in rota.Shifts)
                {
                    if (s.Day == d.DayOfWeek)
                    {
                        slots.Add((d, s));
                    }
                }
            }

            // remove existing assignments in range for this rota
            var existing = _db.ShiftAssignments.Where(sa => sa.RotaId == rotaId && sa.Date >= start.Date && sa.Date <= end.Date).ToList();
            _db.ShiftAssignments.RemoveRange(existing);

            if (!doctorIds.Any())
            {
                // create empty assignments
                foreach (var slot in slots)
                {
                    _db.ShiftAssignments.Add(new Rota2.Models.ShiftAssignment
                    {
                        RotaId = rotaId,
                        ShiftId = slot.shift.Id,
                        Date = slot.date,
                        UserId = null
                    });
                }
            }
            else
            {
                // allocate based on WTE weights and spread assignments over the period
                // build doctor list with weights (WTE) and exclude inactive users
                var docs = rota.RotaDoctors
                    .Where(rd => rd.User != null && rd.User.Active)
                    .Select(rd => new {
                        UserId = rd.UserId,
                        Wte = rd.User?.Wte ?? 1.0m
                    })
                    .ToList();

                // if no active doctors, create empty assignments
                if (!docs.Any())
                {
                    foreach (var slot in slots)
                    {
                        _db.ShiftAssignments.Add(new Rota2.Models.ShiftAssignment
                        {
                            RotaId = rotaId,
                            ShiftId = slot.shift.Id,
                            Date = slot.date,
                            UserId = null
                        });
                    }
                    _db.SaveChanges();
                    return;
                }

                // load leave requests for doctors in the window to determine availability
                var leaveList = _db.LeaveRequests
                    .Where(l => docs.Select(d => d.UserId).Contains(l.UserId) && l.EndDate >= start.Date && l.StartDate <= end.Date)
                    .ToList();

                // compute availability (number of eligible slots) for each doctor
                var totalSlots = slots.Count;
                var availCounts = new Dictionary<int,int>();
                foreach (var d in docs)
                {
                    var uid = d.UserId;
                    var avail = 0;
                    for (int si = 0; si < slots.Count; si++)
                    {
                        var slot = slots[si];
                        var onLeave = leaveList.Any(l => l.UserId == uid && l.StartDate.Date <= slot.date.Date && l.EndDate.Date >= slot.date.Date);
                        var runsOvernight = slot.shift.End <= slot.shift.Start;
                        if (!onLeave && runsOvernight)
                        {
                            var dayBefore = slot.date.Date.AddDays(1);
                            onLeave = leaveList.Any(l => l.UserId == uid && l.StartDate.Date == dayBefore);
                        }
                        if (!onLeave) avail++;
                    }
                    availCounts[uid] = avail;
                }

                // compute initial fractional targets based on WTE across period
                var totalWte = docs.Sum(d => (double)d.Wte);
                if (totalWte <= 0) totalWte = docs.Count > 0 ? docs.Count : 1.0;
                var fractionalTargets = docs.ToDictionary(d => d.UserId, d => (double)totalSlots * ((double)d.Wte / totalWte));

                // convert fractional targets (based on full-period WTE) into integer targets using largest remainders
                var floorTargets = fractionalTargets.ToDictionary(kv => kv.Key, kv => (int)Math.Floor(kv.Value));
                var remainders = fractionalTargets.ToDictionary(kv => kv.Key, kv => kv.Value - Math.Floor(kv.Value));
                var sumFloors = floorTargets.Values.Sum();
                var toAssign = totalSlots - sumFloors;
                var orderedByRemainder = remainders.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Select(kv => kv.Key).ToList();
                var intTargets = new Dictionary<int,int>(floorTargets);
                foreach (var id in orderedByRemainder)
                {
                    if (toAssign <= 0) break;
                    intTargets[id] = intTargets.GetValueOrDefault(id, 0) + 1;
                    toAssign--;
                }

                // mutable tracking for assigned counts and last assigned index
                var docList = docs.Select(d => (UserId: d.UserId, Wte: (double)d.Wte, Assigned: 0, LastAssigned: -100000)).ToList();

                // New assignment strategy:
                // - For each doctor compute the list of available slot indices (not on leave / not excluded by overnight rule)
                // - Use integer targets computed above (intTargets)
                // - Assign for each doctor up to min(target, availableSlots) positions spaced roughly evenly across their available slots
                // - Resolve conflicts by picking the nearest unassigned available slot when a desired slot is already taken
                var assignments = new int?[slots.Count]; // null = unassigned

                // build availability indices per doctor
                var availIndices = docs.ToDictionary(d => d.UserId, d => new List<int>());
                for (int si = 0; si < slots.Count; si++)
                {
                    var slot = slots[si];
                    foreach (var d in docs)
                    {
                        var uid = d.UserId;
                        var onLeave = leaveList.Any(l => l.UserId == uid && l.StartDate.Date <= slot.date.Date && l.EndDate.Date >= slot.date.Date);
                        var runsOvernight = slot.shift.End <= slot.shift.Start;
                        if (!onLeave && runsOvernight)
                        {
                            var dayBefore = slot.date.Date.AddDays(1);
                            onLeave = leaveList.Any(l => l.UserId == uid && l.StartDate.Date == dayBefore);
                        }
                        if (!onLeave)
                        {
                            availIndices[uid].Add(si);
                        }
                    }
                }

                // Order doctors for placement: prioritize those with highest target/availability ratio (those needing densest packing)
                var doctorOrder = docs.Select(d => d.UserId)
                    .OrderByDescending(uid => (double)intTargets.GetValueOrDefault(uid, 0) / Math.Max(1, (availIndices.GetValueOrDefault(uid)?.Count) ?? 0))
                    .ThenBy(uid => uid)
                    .ToList();

                foreach (var uid in doctorOrder)
                {
                    var target = intTargets.GetValueOrDefault(uid, 0);
                    var available = availIndices.GetValueOrDefault(uid);
                    if (available == null || available.Count == 0 || target <= 0) continue;

                    var assignCount = Math.Min(target, available.Count);
                    // spacing step over available indices
                    var step = (double)available.Count / assignCount;
                    for (int k = 0; k < assignCount; k++)
                    {
                        var desiredPos = (int)Math.Round((k + 0.5) * step - 0.5);
                        if (desiredPos < 0) desiredPos = 0;
                        if (desiredPos >= available.Count) desiredPos = available.Count - 1;
                        // map to slot index
                        var slotIndex = available[desiredPos];
                        // if already assigned, search nearest available spot in this doctor's available list
                        if (assignments[slotIndex].HasValue)
                        {
                            int found = -1;
                            int radius = 1;
                            while (found == -1)
                            {
                                var left = desiredPos - radius;
                                var right = desiredPos + radius;
                                bool any = false;
                                if (left >= 0)
                                {
                                    any = true;
                                    var sidx = available[left];
                                    if (!assignments[sidx].HasValue) { found = sidx; break; }
                                }
                                if (right < available.Count)
                                {
                                    any = true;
                                    var sidx = available[right];
                                    if (!assignments[sidx].HasValue) { found = sidx; break; }
                                }
                                if (!any) break;
                                radius++;
                            }
                            if (found == -1) continue; // cannot place this doctor's k-th slot
                            slotIndex = found;
                        }

                        // assign slotIndex to uid
                        assignments[slotIndex] = uid;
                        var di = docList.FindIndex(d => d.UserId == uid);
                        var chosen = docList[di];
                        docList[di] = (chosen.UserId, chosen.Wte, chosen.Assigned + 1, slotIndex);
                    }
                }

                // write assignments to DB (leave any remaining unassigned slots as null)
                for (int si = 0; si < slots.Count; si++)
                {
                    _db.ShiftAssignments.Add(new Rota2.Models.ShiftAssignment
                    {
                        RotaId = rotaId,
                        ShiftId = slots[si].shift.Id,
                        Date = slots[si].date,
                        UserId = assignments[si]
                    });
                }
            }
            _db.SaveChanges();
        }

        public void UpdateAssignment(int assignmentId, int? userId)
        {
            var a = _db.ShiftAssignments.Find(assignmentId);
            if (a == null) return;
            a.UserId = userId;
            _db.SaveChanges();
        }

        public Rota Create(Rota rota, IEnumerable<int> doctorIds, IEnumerable<int> adminIds)
        {
            // detach any ids on shifts
            foreach (var s in rota.Shifts)
            {
                s.Id = 0;
            }
            _db.Rotas.Add(rota);
            _db.SaveChanges();
            foreach (var d in doctorIds.Distinct())
            {
                _db.RotaDoctors.Add(new RotaDoctor { RotaId = rota.Id, UserId = d });
            }
            foreach (var a in adminIds.Distinct())
            {
                _db.RotaAdmins.Add(new RotaAdmin { RotaId = rota.Id, UserId = a });
            }
            _db.SaveChanges();
            // invalidate rota-admin cache entries for added admins
            try
            {
                foreach (var a in adminIds.Distinct()) _cache?.Remove($"rotaadmin:{a}");
            }
            catch { }
            return rota;
        }

        public void Update(Rota rota, IEnumerable<int> doctorIds, IEnumerable<int> adminIds)
        {
            var existing = _db.Rotas.Include(r => r.Shifts).Include(r => r.RotaDoctors).Include(r => r.RotaAdmins).SingleOrDefault(r => r.Id == rota.Id);
            if (existing == null) return;
            existing.Name = rota.Name;

            // replace shifts
            _db.Shifts.RemoveRange(existing.Shifts);
            foreach (var s in rota.Shifts)
            {
                s.Id = 0;
                s.RotaId = existing.Id;
                _db.Shifts.Add(s);
            }

            // replace doctors
            _db.RotaDoctors.RemoveRange(existing.RotaDoctors);
            foreach (var d in doctorIds.Distinct())
            {
                _db.RotaDoctors.Add(new RotaDoctor { RotaId = existing.Id, UserId = d });
            }

            // replace admins
            _db.RotaAdmins.RemoveRange(existing.RotaAdmins);
            foreach (var a in adminIds.Distinct())
            {
                _db.RotaAdmins.Add(new RotaAdmin { RotaId = existing.Id, UserId = a });
            }

            _db.SaveChanges();
        }

        public void Delete(int id)
        {
            // collect affected admin ids to invalidate cache
            var adminIds = _db.RotaAdmins.Where(ra => ra.RotaId == id).Select(ra => ra.UserId).ToList();
            var existing = _db.Rotas.Find(id);
            if (existing == null) return;
            _db.Rotas.Remove(existing);
            _db.SaveChanges();
            try
            {
                foreach (var uid in adminIds) _cache?.Remove($"rotaadmin:{uid}");
            }
            catch { }
        }

        public Rota DuplicateRota(int sourceRotaId, string newName)
        {
            var src = _db.Rotas.Include(r => r.Shifts).SingleOrDefault(r => r.Id == sourceRotaId);
            if (src == null) return null!;
            var copy = new Rota
            {
                Name = newName
            };
            foreach (var s in src.Shifts)
            {
                copy.Shifts.Add(new Shift { Day = s.Day, Start = s.Start, End = s.End });
            }
            _db.Rotas.Add(copy);
            _db.SaveChanges();
            return copy;
        }

        public bool IsRotaAdmin(int userId)
        {
            if (userId <= 0) return false;
            var key = $"rotaadmin:{userId}";
            return _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return _db.RotaAdmins.Any(ra => ra.UserId == userId);
            });
        }
    }
}
