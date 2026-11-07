using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common.Configuration;
using Infrastructure.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Infrastructure.Services
{
    public class TwilioSmsService : ISmsService
    {
        private readonly TwilioSettings _settings;
        private readonly ILogger<TwilioSmsService> _logger;

        public TwilioSmsService(IOptions<TwilioSettings> settings, ILogger<TwilioSmsService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            if (!string.IsNullOrEmpty(_settings.AccountSid) && !string.IsNullOrEmpty(_settings.AuthToken))
            {
                TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
            }
        }

        public async Task<bool> SendAsync(string to, string messageBody)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.AccountSid) || string.IsNullOrEmpty(_settings.AuthToken))
                {
                    _logger.LogWarning("Twilio credentials not configured, skipping SMS to {To}", to);
                    return false;
                }

                if (string.IsNullOrEmpty(_settings.FromNumber))
                {
                    _logger.LogWarning("Twilio FromNumber not configured, skipping SMS to {To}", to);
                    return false;
                }

                var messageResource = await MessageResource.CreateAsync(
                    body: messageBody,
                    from: new PhoneNumber(_settings.FromNumber),
                    to: new PhoneNumber(to)
                );

                _logger.LogInformation("SMS sent to {To}: {Sid}", to, messageResource.Sid);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {To}", to);
                return false;
            }
        }

        public async Task<bool> SendTemplateAsync(string to, string templateName, object model, string culture = "ar")
        {
            _logger.LogWarning("SendTemplateAsync called but templating should be done by caller. Template: {TemplateName}", templateName);
            return false;
        }

        public async Task<bool> SendBulkAsync(IEnumerable<string> recipients, string messageBody)
        {
            var results = new List<bool>();
            foreach (var recipient in recipients)
            {
                var result = await SendAsync(recipient, messageBody);
                results.Add(result);
            }
            return results.All(r => r);
        }
    }
}
