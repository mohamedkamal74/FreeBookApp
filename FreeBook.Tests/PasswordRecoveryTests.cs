using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class PasswordRecoveryTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string GenericSentMessage =
        "If that email address is registered, a password reset link has been sent.";
    private const string ResetErrorMessage =
        "We could not reset your password. The link may be invalid or expired.";

    private readonly CustomWebApplicationFactory _factory;

    public PasswordRecoveryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("en", "Forgot Password?")]
    [InlineData("ar", "نسيت كلمة المرور؟")]
    public async Task ForgotPasswordPage_IsLocalized(string culture, string expectedText)
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync($"/Admin/Accounts/ForgotPassword?culture={culture}");

        Assert.Contains(expectedText, WebUtility.HtmlDecode(html));
        Assert.Contains("name=\"Email\"", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ShowsGenericMessageAndSendsNothing()
    {
        _factory.EmailService.Messages.Clear();
        var client = CreateClientWithoutRedirects();
        var token = await GetAntiForgeryTokenAsync(client, "/Admin/Accounts/ForgotPassword?culture=en");

        var response = await client.PostAsync("/Admin/Accounts/ForgotPassword?culture=en", Form(
            ("Email", $"missing-{Guid.NewGuid():N}@test.local"),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Accounts/ForgotPassword", response.Headers.Location!.OriginalString);

        var afterPost = await client.GetStringAsync(response.Headers.Location.OriginalString);
        Assert.Contains(JsonSerializer.Serialize(GenericSentMessage), afterPost);
        Assert.Empty(_factory.EmailService.Messages);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_ResetsPasswordThroughEmailedLink()
    {
        _factory.EmailService.Messages.Clear();
        var email = $"reset-{Guid.NewGuid():N}@test.local";
        const string oldPassword = "Old@12345";
        const string newPassword = "New@12345";
        var userId = await SeedUserAsync(email, oldPassword);
        var client = CreateClientWithoutRedirects();

        var requestToken = await GetAntiForgeryTokenAsync(client, "/Admin/Accounts/ForgotPassword?culture=en");
        var requestResponse = await client.PostAsync("/Admin/Accounts/ForgotPassword?culture=en", Form(
            ("Email", email),
            ("__RequestVerificationToken", requestToken)));
        Assert.Equal(HttpStatusCode.Redirect, requestResponse.StatusCode);

        var sent = Assert.Single(_factory.EmailService.Messages);
        Assert.Equal(email, sent.To);
        Assert.Equal("Reset your Bookify password", sent.Subject);
        var resetUrl = sent.TextBody!.Split('\n').Last().Trim();
        Assert.Contains("/Admin/Accounts/ResetPassword", resetUrl);

        var query = QueryHelpers.ParseQuery(new Uri(resetUrl).Query);
        Assert.Equal(userId, query["userId"].ToString());
        Assert.False(string.IsNullOrWhiteSpace(query["code"].ToString()));

        var resetHtml = await client.GetStringAsync(resetUrl);
        var resetToken = ExtractAntiForgeryToken(resetHtml);
        Assert.False(string.IsNullOrWhiteSpace(resetToken));

        var resetResponse = await client.PostAsync(resetUrl, Form(
            ("UserId", userId),
            ("Code", query["code"].ToString()),
            ("NewPassword", newPassword),
            ("ConfirmPassword", newPassword),
            ("__RequestVerificationToken", resetToken!)));
        Assert.Equal(HttpStatusCode.Redirect, resetResponse.StatusCode);
        var loginUrl = resetResponse.Headers.Location!.OriginalString;
        Assert.True(
            loginUrl is "/Admin" or "/Admin/Accounts/Login",
            $"Unexpected password-reset redirect: {loginUrl}");

        var afterReset = await client.GetStringAsync(loginUrl);
        Assert.Contains("name=\"Email\"", afterReset);
        Assert.Contains(
            JsonSerializer.Serialize("Your password has been reset. Please sign in with the new password."),
            afterReset);

        Assert.Equal(HttpStatusCode.OK, await TryLoginAsync(client, email, oldPassword));
        Assert.Equal(HttpStatusCode.Redirect, await TryLoginAsync(client, email, newPassword));
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ShowsLocalizedError()
    {
        var email = $"invalid-{Guid.NewGuid():N}@test.local";
        var userId = await SeedUserAsync(email, "Old@12345");
        var client = CreateClientWithoutRedirects();
        var url = "/Admin/Accounts/ResetPassword?culture=en";

        var token = await GetAntiForgeryTokenAsync(client, $"{url}&userId={userId}&code=not-a-valid-token");
        var response = await client.PostAsync(url, Form(
            ("UserId", userId),
            ("Code", "not-a-valid-token"),
            ("NewPassword", "New@12345"),
            ("ConfirmPassword", "New@12345"),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(ResetErrorMessage, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    private WebApplicationFactoryClientOptions CreateClientOptions() => new()
    {
        AllowAutoRedirect = false
    };

    private HttpClient CreateClientWithoutRedirects() => _factory.CreateClient(CreateClientOptions());

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)));

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        return ExtractAntiForgeryToken(html)!;
    }

    private static string? ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static async Task<HttpStatusCode> TryLoginAsync(HttpClient client, string email, string password)
    {
        var token = await GetAntiForgeryTokenAsync(client, "/Admin/Accounts/Login?culture=en");
        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en", Form(
            ("Email", email),
            ("Password", password),
            ("RememberMe", "false"),
            ("__RequestVerificationToken", token)));
        return response.StatusCode;
    }

    private async Task<string> SeedUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            Name = "Password Recovery User",
            ActiveUser = true,
            EmailConfirmed = true,
            ImageUser = string.Empty
        };

        var created = await userManager.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
        return user.Id;
    }
}
