using System.Net;
using Domin.Entity;
using FreeBook.Resources;
using Infrastructure.Common.Interfaces;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace FreeBook.Services
{
    /// <summary>
    /// Bridges ASP.NET Identity's email tokens to the existing MailKit-backed
    /// <see cref="IEmailService"/>. The link is passed through unchanged.
    /// </summary>
    public sealed class IdentityEmailSender : IEmailSender<ApplicationUser>
    {
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<ResourceWeb> _localizer;

        public IdentityEmailSender(IEmailService emailService, IStringLocalizer<ResourceWeb> localizer)
        {
            _emailService = emailService;
            _localizer = localizer;
        }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
            SendLinkAsync(email, confirmationLink, "msgConfirmationEmailSubject", "msgConfirmationEmailBody");

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            SendLinkAsync(email, resetLink, "msgResetEmailSubject", "msgResetEmailBody");

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            var subject = _localizer["msgResetEmailSubject"].Value;
            var intro = _localizer["msgResetCodeEmailBody"].Value;
            var html = $"<p>{WebUtility.HtmlEncode(intro)}</p><p><code>{WebUtility.HtmlEncode(resetCode)}</code></p>";
            var text = $"{intro}\n{resetCode}";
            return _emailService.SendAsync(email, subject, html, text);
        }

        private Task SendLinkAsync(string email, string link, string subjectKey, string bodyKey)
        {
            var subject = _localizer[subjectKey].Value;
            var intro = _localizer[bodyKey].Value;
            var linkText = _localizer["lbOpenLink"].Value;
            var html =
                $"<p>{WebUtility.HtmlEncode(intro)}</p>" +
                $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(linkText)}</a></p>";
            var text = $"{intro}\n{link}";
            return _emailService.SendAsync(email, subject, html, text);
        }
    }
}
