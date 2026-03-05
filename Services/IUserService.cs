using Rota2.Models;

namespace Rota2.Services
{
    public interface IUserService
    {
        User? Authenticate(string email, string password);
        User CreateUser(User user, string password);
        User CreateUserNoPassword(User user);
        IEnumerable<User> GetAllUsers();
        User? GetById(int id);
        void UpdatePassword(int id, string newPassword);
        void UpdateUser(User user);
        IEnumerable<Rota2.Models.LeaveRequest> GetLeavesForUser(int userId);
        Rota2.Models.LeaveRequest CreateLeave(Rota2.Models.LeaveRequest leave);
        void UpdateLeave(Rota2.Models.LeaveRequest leave);
        void DeleteLeave(int id);
    }
}
