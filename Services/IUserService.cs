using Rota2.Models;

namespace Rota2.Services
{
    public interface IUserService
    {
        User? Authenticate(string email, string password);
        User CreateUser(User user, string password);
        IEnumerable<User> GetAllUsers();
        User? GetById(int id);
        void UpdatePassword(int id, string newPassword);
        void UpdateUser(User user);
    }
}
