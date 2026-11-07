using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Common.Configuration;
using Infrastructure.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, string textBody = null)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                if (!string.IsNullOrEmpty(textBody))
                {
                    bodyBuilder.TextBody = textBody;
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
                {
                    await client.AuthenticateAsync(_settings.Username, _settings.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
                throw;
            }
        }

        public async Task SendTemplateAsync(string to, string templateName, object model, string culture = "ar")
        {
            // This method is kept for interface compatibility but templating should be done by caller
            _logger.LogWarning("SendTemplateAsync called but templating should be done by caller. Template: {TemplateName}", templateName);
            throw new NotSupportedException("Templating should be done by the caller. Use SendAsync with pre-rendered content.");
        }

        public async Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody)
        {
            foreach (var recipient in recipients)
            {
                await SendAsync(recipient, subject, htmlBody);
            }
        }
    }
}
