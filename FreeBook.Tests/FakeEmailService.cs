using System.Collections.Concurrent;
using Infrastructure.Common.Interfaces;

namespace FreeBook.Tests;

public sealed class CapturedEmail
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
}

public sealed class FakeEmailService : IEmailService
{
    public ConcurrentQueue<CapturedEmail> Messages { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        Messages.Enqueue(new CapturedEmail
        {
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        });
        return Task.CompletedTask;
    }

    public Task SendTemplateAsync(string to, string templateName, object model, string culture = "ar") =>
        Task.CompletedTask;

    public Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody) =>
        Task.CompletedTask;
}
