using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;

namespace ECommercePlatform.Services
{
    public class EmailService
    {
        private readonly string _emailAccount;
        private readonly string _emailPassword;

        public EmailService()
        {
            _emailAccount = Environment.GetEnvironmentVariable("EMAIL_ACCOUNT") ?? throw new Exception("Missing EMAIL_ACCOUNT env variable");
            _emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? throw new Exception("Missing EMAIL_PASSWORD env variable");
        }

        public void SendComplainMail(string subject, string body, string to = "testproject9487@gmail.com")
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Server", _emailAccount));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            client.Authenticate(_emailAccount, _emailPassword);
            client.Send(message);
            client.Disconnect(true);
        }
    }
}