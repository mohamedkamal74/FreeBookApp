using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody, string? textBody = null);
        Task SendTemplateAsync(string to, string templateName, object model, string culture = "ar");
        Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody);
    }
}
