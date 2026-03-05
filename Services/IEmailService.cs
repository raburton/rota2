using System.Threading.Tasks;

namespace Rota2.Services
{
    public interface IEmailService
    {
        Task SendNewUserNotificationAsync(string toEmail, string name);
    }
}
