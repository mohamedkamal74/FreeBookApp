using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Common.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendAsync(string to, string message);
        Task<bool> SendTemplateAsync(string to, string templateName, object model, string culture = "ar");
        Task<bool> SendBulkAsync(IEnumerable<string> recipients, string message);
    }
}
