using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace KayipEsyaOtomasyonu.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<SmtpSettings> settings,
            ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_settings.Host))
                {
                    _logger.LogWarning(
                        "SMTP Host ayarlanmamis. E-posta gonderilmedi: To={To}, Subject={Subject}\nBody:{Body}",
                        toEmail, subject, htmlBody);
                    return;
                }

                var smtp = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    Timeout = 10000
                };

                var from = new MailAddress(
                    string.IsNullOrWhiteSpace(_settings.From) ? _settings.Username : _settings.From,
                    "Kayip Esya Yonetim Sistemi");

                var to = new MailAddress(toEmail);

                using var message = new MailMessage(from, to)
                {
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                await smtp.SendMailAsync(message);
                _logger.LogInformation("E-posta gonderildi: To={To} Subject={Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta gonderim hatasi: To={To} Subject={Subject}", toEmail, subject);
                throw;
            }
        }
    }

    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? From { get; set; }
    }
}
