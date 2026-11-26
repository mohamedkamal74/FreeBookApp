using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class ChangePasswordTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string OriginalPassword = "Change@12345";
    private const string NewPassword = "Changed@12345";
    private const string OtherPassword = "Other@12345";

    private readonly CustomWebApplicationFactory _factory;

    public ChangePasswordTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_IsRedirectedToLogin_FromChangePasswordPage()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Admin/Accounts/ChangePassword?culture=en");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Accounts/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SignedInUser_SeesSelfServiceForm_WithExpectedHooksAndLocalization()
    {
        var (email, _) = await SeedUserAsync("User", OriginalPassword);
        var client = await CreateLoggedInClientAsync(email, OriginalPassword);

        var english = await client.GetStringAsync("/Admin/Accounts/ChangePassword?culture=en");
        Assert.Contains("dir=\"ltr\"", english);
        Assert.Contains("Current password", english);
        Assert.Contains("name=\"CurrentPassword\"", english);
        Assert.Contains("name=\"NewPassword\"", english);
        Assert.Contains("name=\"ConfirmPassword\"", english);
        Assert.Contains("id=\"changePasswordModalBtnSave\"", english);
        Assert.Contains("id=\"userPassId\"", english);
        Assert.DoesNotContain("name=\"Id\"", english);

        var arabic = WebUtility.HtmlDecode(
            await client.GetStringAsync("/Admin/Accounts/ChangePassword?culture=ar"));
        Assert.Contains("dir=\"rtl\"", arabic);
        Assert.Contains("كلمة المرور الحالية", arabic);
    }

    [Fact]
    public async Task MissingCurrentPassword_IsRejected_AndPasswordIsUnchanged()
    {
        var (email, _) = await SeedUserAsync("User", OriginalPassword);
        var client = await CreateLoggedInClientAsync(email, OriginalPassword);
        var token = ExtractToken(await client.GetStringAsync("/Admin/Accounts/ChangePassword?culture=en"));

        var response = await client.PostAsync("/Admin/Accounts/ChangePassword?culture=en",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["CurrentPassword"] = string.Empty,
                ["NewPassword"] = NewPassword,
                ["ConfirmPassword"] = NewPassword,
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await CheckPasswordAsync(email, OriginalPassword));
        Assert.False(await CheckPasswordAsync(email, NewPassword));
    }

    [Fact]
    public async Task WrongCurrentPassword_IsRejected_AndPasswordIsUnchanged()
    {
        var (email, _) = await SeedUserAsync("User", OriginalPassword);
        var client = await CreateLoggedInClientAsync(email, OriginalPassword);
        var token = ExtractToken(await client.GetStringAsync("/Admin/Accounts/ChangePassword?culture=en"));

        var response = await client.PostAsync("/Admin/Accounts/ChangePassword?culture=en",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["CurrentPassword"] = "Wrong@12345",
                ["NewPassword"] = NewPassword,
                ["ConfirmPassword"] = NewPassword,
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Accounts/ChangePassword", response.Headers.Location!.OriginalString);
        Assert.True(await CheckPasswordAsync(email, OriginalPassword));
        Assert.False(await CheckPasswordAsync(email, NewPassword));

        var afterPost = await client.GetStringAsync("/Admin/Accounts/ChangePassword?culture=en");
        Assert.Contains(JsonSerializer.Serialize("Password not changed"), afterPost);
    }

    [Fact]
    public async Task PostedTargetId_IsIgnored_AndOnlySignedInUserPasswordChanges()
    {
        var (selfEmail, _) = await SeedUserAsync("User", OriginalPassword);
        var (otherEmail, otherId) = await SeedUserAsync("User", OtherPassword);
        var client = await CreateLoggedInClientAsync(selfEmail, OriginalPassword);
        var token = ExtractToken(await client.GetStringAsync("/Admin/Accounts/ChangePassword?culture=en"));

        var response = await client.PostAsync("/Admin/Accounts/ChangePassword?culture=en",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = otherId,
                ["changePassword.Id"] = otherId,
                ["CurrentPassword"] = OriginalPassword,
                ["NewPassword"] = NewPassword,
                ["ConfirmPassword"] = NewPassword,
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Accounts/ChangePassword", response.Headers.Location!.OriginalString);

        Assert.False(await CheckPasswordAsync(selfEmail, OriginalPassword));
        Assert.True(await CheckPasswordAsync(selfEmail, NewPassword));
        Assert.True(await CheckPasswordAsync(otherEmail, OtherPassword));
        Assert.False(await CheckPasswordAsync(otherEmail, NewPassword));

        var afterPost = await client.GetStringAsync("/Admin/Accounts/ChangePassword?culture=en");
        Assert.Contains(JsonSerializer.Serialize("Password changed successfully"), afterPost);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<HttpClient> CreateLoggedInClientAsync(string email, string password)
    {
        var client = CreateClient();
        var token = ExtractToken(await client.GetStringAsync("/Admin/Accounts/Login?culture=en"));
        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = password,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Unexpected login status: {response.StatusCode}");
        return client;
    }

    private async Task<(string Email, string Id)> SeedUserAsync(string role, string password)
    {
        var email = $"change-password-{Guid.NewGuid():N}@test.local";
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            Name = "Change Password User",
            ActiveUser = true,
            EmailConfirmed = true,
            ImageUser = string.Empty
        };

        var created = await userManager.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        return (email, user.Id);
    }

    private async Task<bool> CheckPasswordAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        return await userManager.CheckPasswordAsync(user!, password);
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
