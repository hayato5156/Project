using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Threading.Tasks;
//using (var client = new SmtpClient(_smtpServer, _smtpPort));

namespace ECommercePlatform.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;

        public EmailService(string smtpServer, int smtpPort, string smtpUsername, string smtpPassword)
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _smtpUsername = smtpUsername;
            _smtpPassword = smtpPassword;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort)) // Added 'using' statement to define 'client'
            {
                client.Credentials = new System.Net.NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = true; // 如果你的 SMTP 伺服器需要 SSL
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // 如果郵件內容是 HTML
                };
                mailMessage.To.Add(toEmail);
                try
                {
                    await client.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    // 在這裡記錄錯誤或處理發送失敗的情況
                    Console.WriteLine($"發送郵件失敗: {ex.Message}");
                }
            }
        }
        // 可以加入其他郵件相關的方法，例如發送訂單確認信、註冊驗證信等
    }
}
