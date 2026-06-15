using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PharmaCheck.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            // Đọc cấu hình từ appsettings.json
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var appPassword = _configuration["EmailSettings:AppPassword"]; // Mật khẩu ứng dụng 16 ký tự của Gmail

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(fromEmail, "PharmaCheck Security");
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = htmlMessage;
                message.IsBodyHtml = true; // Cho phép hiển thị giao diện HTML (nút bấm, màu sắc)

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(fromEmail, appPassword);

                    await client.SendMailAsync(message);
                }
            }
        }
    }
}