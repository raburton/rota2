using Microsoft.EntityFrameworkCore;
using Rota2.Data;
using Rota2.Models;
using System.Collections.Generic;
using System.Linq;

namespace Rota2.Services
{
    public class RotaService : IRotaService
    {
        private readonly AppDbContext _db;
        public RotaService(AppDbContext db)
        {
            _db = db;
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

                // convert to mutable list of tuples: (UserId, WteDouble, AssignedCount, LastAssignedIndex)
                var docList = docs.Select(d => (UserId: d.UserId, Wte: (double)d.Wte, Assigned: 0, LastAssigned: -100000)).ToList();

                var totalWte = docList.Sum(d => d.Wte);
                // if totalWte is zero (shouldn't happen) fall back to equal weights
                if (totalWte <= 0)
                {
                    for (int i = 0; i < docList.Count; i++) docList[i] = (docList[i].UserId, 1.0, 0, -100000);
                    totalWte = docList.Sum(d => d.Wte);
                }

                for (var si = 0; si < slots.Count; si++)
                {
                    var slot = slots[si];
                    // choose doctor with minimal (Assigned / Wte) ratio to keep assignment proportional
                    int bestIdx = -1;
                    double bestRatio = double.MaxValue;
                    for (int di = 0; di < docList.Count; di++)
                    {
                        var d = docList[di];
                        if (d.Wte <= 0) continue; // skip zero weight
                        var ratio = (double)d.Assigned / d.Wte;
                        if (ratio < bestRatio - 1e-9)
                        {
                            bestRatio = ratio;
                            bestIdx = di;
                        }
                        else if (Math.Abs(ratio - bestRatio) < 1e-9)
                        {
                            // tie-break: prefer the doctor who was assigned least recently (smaller LastAssigned)
                            if (bestIdx == -1 || d.LastAssigned < docList[bestIdx].LastAssigned)
                            {
                                bestIdx = di;
                            }
                        }
                    }

                    // if no suitable doctor found (all Wte zero), fallback to round-robin
                    if (bestIdx == -1)
                    {
                        bestIdx = si % docList.Count;
                    }

                    var chosen = docList[bestIdx];
                    // update assignment counts
                    docList[bestIdx] = (chosen.UserId, chosen.Wte, chosen.Assigned + 1, si);

                    _db.ShiftAssignments.Add(new Rota2.Models.ShiftAssignment
                    {
                        RotaId = rotaId,
                        ShiftId = slot.shift.Id,
                        Date = slot.date,
                        UserId = chosen.UserId
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
            var existing = _db.Rotas.Find(id);
            if (existing == null) return;
            _db.Rotas.Remove(existing);
            _db.SaveChanges();
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
    }
}
