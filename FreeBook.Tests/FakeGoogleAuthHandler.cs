using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreeBook.Tests;

internal sealed class FakeGoogleAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    internal const string SchemeName = "Google";
    internal const string Email = "google-user@example.com";
    internal const string DisplayName = "Google Test User";

    public FakeGoogleAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "google-test-provider-key"),
            new Claim(ClaimTypes.Name, DisplayName),
            new Claim(ClaimTypes.Email, Email)
        };

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Email);
        var principal = new ClaimsPrincipal(identity);

        properties.Items[".AuthScheme"] = SchemeName;
        await Context.SignInAsync(IdentityConstants.ExternalScheme, principal, properties);
        Response.Redirect(properties.RedirectUri ?? "/");
    }
}
