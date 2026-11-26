using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FreeBook.Tests;

public class LocalizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LocalizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SetCulture_WithoutAntiforgeryToken_IsRejected()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/Localization/SetCulture", Form("ar", "/"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetCulture_WithValidToken_SetsCultureCookieAndAppliesIt()
    {
        var client = CreateClient();
        var token = ExtractToken(await client.GetStringAsync("/?culture=en"));

        var response = await client.PostAsync("/Localization/SetCulture", Form("ar", "/", token));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);

        var cultureCookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(
                CookieRequestCultureProvider.DefaultCookieName + "=",
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains("uic=ar", Uri.UnescapeDataString(cultureCookie));

        var afterRedirect = await client.GetStringAsync(response.Headers.Location);
        Assert.Contains("dir=\"rtl\"", afterRedirect);
    }

    [Fact]
    public async Task SetCulture_WithExternalReturnUrl_FallsBackToHome()
    {
        var client = CreateClient();
        var token = ExtractToken(await client.GetStringAsync("/?culture=en"));

        var response = await client.PostAsync(
            "/Localization/SetCulture",
            Form("ar", "https://evil.example/phishing", token));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private static FormUrlEncodedContent Form(string culture, string returnUrl, string? token = null)
    {
        var values = new Dictionary<string, string>
        {
            ["culture"] = culture,
            ["returnUrl"] = returnUrl
        };

        if (token is not null)
        {
            values["__RequestVerificationToken"] = token;
        }

        return new FormUrlEncodedContent(values);
    }

    private static string ExtractToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, "No antiforgery token found");
        return match.Groups[1].Value;
    }
}
