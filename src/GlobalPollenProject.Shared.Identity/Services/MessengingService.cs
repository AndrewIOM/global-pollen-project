using System.Threading.Tasks;

namespace GlobalPollenProject.Shared.Identity.Services
{

    public class AuthEmailMessageSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Plug in your email service here to send an email.
            return Task.FromResult(0);
        }
    }
}