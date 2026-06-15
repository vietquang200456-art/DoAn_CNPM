using System.Threading.Tasks;

namespace PharmaCheck.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}