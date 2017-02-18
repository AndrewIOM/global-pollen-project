using System.Threading.Tasks;

namespace GlobalPollenProject.Shared.Identity.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}