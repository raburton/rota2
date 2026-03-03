using Microsoft.EntityFrameworkCore;
using Rota2.Data;
using Rota2.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rota2.Services
{
    public class SwapService : ISwapService
    {
        private readonly AppDbContext _db;
        public SwapService(AppDbContext db)
        {
            _db = db;
        }

        public SwapRequest Create(SwapRequest req)
        {
            req.Status = SwapStatus.Pending;
            req.CreatedAt = DateTime.UtcNow;
            _db.SwapRequests.Add(req);
            _db.SaveChanges();
            return req;
        }

        public IEnumerable<SwapRequest> GetIncoming(int userId)
        {
            return _db.SwapRequests.AsNoTracking().Where(s => s.ToUserId == userId).OrderByDescending(s => s.CreatedAt).ToList();
        }

        public IEnumerable<SwapRequest> GetOutgoing(int userId)
        {
            return _db.SwapRequests.AsNoTracking().Where(s => s.FromUserId == userId).OrderByDescending(s => s.CreatedAt).ToList();
        }

        public SwapRequest? GetById(int id)
        {
            return _db.SwapRequests.Find(id);
        }

        public void UpdateStatus(int id, SwapStatus status)
        {
            var s = _db.SwapRequests.Find(id);
            if (s == null) return;
            s.Status = status;
            s.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
        }

        public bool Accept(int id, int actingUserId)
        {
            // actingUserId must be the nominated ToUserId
            var s = _db.SwapRequests.Find(id);
            if (s == null) return false;
            if (s.ToUserId != actingUserId) return false;
            if (s.Status != SwapStatus.Pending) return false;

            // parse ids
            var reqIds = s.RequestedAssignments.ToList();
            var offIds = s.OfferedAssignments.ToList();

            using var tx = _db.Database.BeginTransaction();
            try
            {
                // reload assignments for update
                var assignments = _db.ShiftAssignments.Where(a => reqIds.Contains(a.Id) || offIds.Contains(a.Id)).ToList();

                // validate ownership
                foreach (var aid in reqIds)
                {
                    var a = assignments.SingleOrDefault(x => x.Id == aid);
                    if (a == null || a.UserId != s.ToUserId) throw new InvalidOperationException("Requested assignment ownership mismatch");
                }
                foreach (var aid in offIds)
                {
                    var a = assignments.SingleOrDefault(x => x.Id == aid);
                    if (a == null || a.UserId != s.FromUserId) throw new InvalidOperationException("Offered assignment ownership mismatch");
                }

                // perform swaps: requested assignments -> FromUserId, offered assignments -> ToUserId
                foreach (var aid in reqIds)
                {
                    var a = assignments.Single(x => x.Id == aid);
                    a.UserId = s.FromUserId;
                    _db.ShiftAssignments.Update(a);
                }
                foreach (var aid in offIds)
                {
                    var a = assignments.Single(x => x.Id == aid);
                    a.UserId = s.ToUserId;
                    _db.ShiftAssignments.Update(a);
                }

                s.Status = SwapStatus.Accepted;
                s.UpdatedAt = DateTime.UtcNow;
                _db.SwapRequests.Update(s);
                _db.SaveChanges();
                tx.Commit();
                return true;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                return false;
            }
        }
    }
}
