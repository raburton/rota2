using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Rota2.Services
{
    // Simple email sender placeholder - in production replace with real SMTP/email provider
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _log;
        public EmailService(ILogger<EmailService> log)
        {
            _log = log;
        }

        public Task SendNewUserNotificationAsync(string toEmail, string name)
        {
            // Log a notification - this can be replaced with SMTP or third-party provider implementation
            _log.LogInformation("New user created: {Email} ({Name}) - notification placeholder", toEmail, name);
            return Task.CompletedTask;
        }
    }
}
