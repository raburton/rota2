using Microsoft.EntityFrameworkCore;
using Rota2.Data;
using Rota2.Models;

namespace Rota2.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public User? Authenticate(string email, string password)
        {
            var user = _db.Users.SingleOrDefault(u => u.Email == email && u.Active);
            if (user == null) return null;
            if (!PasswordHasher.Verify(user.PasswordHash, password)) return null;
            return user;
        }

        public User CreateUser(User user, string password)
        {
            user.PasswordHash = PasswordHasher.Hash(password);
            _db.Users.Add(user);
            _db.SaveChanges();
            return user;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _db.Users.AsNoTracking().ToList();
        }

        public User? GetById(int id)
        {
            return _db.Users.Find(id);
        }

        public void UpdatePassword(int id, string newPassword)
        {
            var user = _db.Users.Find(id);
            if (user == null) return;
            user.PasswordHash = PasswordHasher.Hash(newPassword);
            _db.SaveChanges();
        }

        public void UpdateUser(User user)
        {
            var existing = _db.Users.Find(user.Id);
            if (existing == null) return;
            existing.Name = user.Name;
            existing.Email = user.Email;
            existing.Role = user.Role;
            existing.Wte = user.Wte;
            existing.Active = user.Active;
            existing.IsGlobalAdmin = user.IsGlobalAdmin;
            _db.SaveChanges();
        }

        public IEnumerable<Rota2.Models.LeaveRequest> GetLeavesForUser(int userId)
        {
            return _db.LeaveRequests.AsNoTracking().Where(l => l.UserId == userId).OrderByDescending(l => l.StartDate).ToList();
        }

        public Rota2.Models.LeaveRequest CreateLeave(Rota2.Models.LeaveRequest leave)
        {
            _db.LeaveRequests.Add(leave);
            _db.SaveChanges();
            return leave;
        }

        public void UpdateLeave(Rota2.Models.LeaveRequest leave)
        {
            var existing = _db.LeaveRequests.Find(leave.Id);
            if (existing == null) return;
            existing.StartDate = leave.StartDate;
            existing.EndDate = leave.EndDate;
            existing.Notes = leave.Notes;
            _db.SaveChanges();
        }

        public void DeleteLeave(int id)
        {
            var existing = _db.LeaveRequests.Find(id);
            if (existing == null) return;
            _db.LeaveRequests.Remove(existing);
            _db.SaveChanges();
        }
    }
}
