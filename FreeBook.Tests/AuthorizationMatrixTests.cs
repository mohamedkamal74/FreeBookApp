using System.Net;
using System.Text.RegularExpressions;
using Domin.Entity;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

/// <summary>
/// Characterization tests for the CURRENT authorization matrix. Several assertions
/// intentionally document gaps that will be closed in later work items; update them
/// together with the behavior change.
/// </summary>
public class AuthorizationMatrixTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Matrix@12345";

    private static readonly string[] AnonymousAllowedUrls =
    {
        "/",
        "/Admin?culture=en",
        "/Admin/Accounts/ForgotPassword?culture=en",
        "/Admin/Accounts/ResetPassword?culture=en&userId=dummy&code=dummy"
    };

    private static readonly string[] AnonymousAdminUrls =
    {
        "/Admin/Dashboard/Index?culture=en",
        "/Admin/Categories/Categories?culture=en",
        "/Admin/Reports/Books?culture=en"
    };

    private static readonly string[] UserAllowedAdminUrls =
    {
        "/Admin/Dashboard/Index?culture=en",
        "/Admin/BooksDashboard/Index?culture=en",
        "/Admin/SubscribersDashboard/Index?culture=en",
        "/Admin/Categories/Categories?culture=en",
        "/Admin/Accounts/Register?culture=en",
        "/Admin/Accounts/Roles?culture=en"
    };

    private static readonly string[] AdminOnlyUrls =
    {
        "/Admin/Reports/Books?culture=en",
        "/Admin/Books/Books?culture=en",
        "/Admin/Authors/Authors?culture=en",
        "/Admin/Subscribers/Subscribers?culture=en",
        "/Admin/Rentals/Rentals?culture=en",
        "/Admin/Notifications/Index?culture=en"
    };

    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationMatrixTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_CanReachPublicAndAuthPages_ButIsRedirectedFromAdminPages()
    {
        var client = CreateClient();

        foreach (var url in AnonymousAllowedUrls)
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        foreach (var url in AnonymousAdminUrls)
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Admin", response.Headers.Location!.OriginalString);
        }
    }

    [Fact]
    public async Task NormalUser_CanReachLegacyAdminPages_ButIsDeniedFromAdminOnlyPages()
    {
        var email = await SeedUserAsync("User");
        var client = await CreateLoggedInClientAsync(email);

        foreach (var url in UserAllowedAdminUrls)
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        foreach (var url in AdminOnlyUrls)
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Admin/Home/Denied", response.Headers.Location!.OriginalString);
        }
    }

    [Fact]
    public async Task Admin_CanReachAllRepresentativePages()
    {
        var email = await SeedUserAsync("Admin");
        var client = await CreateLoggedInClientAsync(email);

        foreach (var url in UserAllowedAdminUrls.Concat(AdminOnlyUrls))
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task NormalUser_SeesAdminSidebarInAdminLayout_ButNotPublicAdminLinks()
    {
        var email = await SeedUserAsync("User");
        var client = await CreateLoggedInClientAsync(email);

        var adminLayout = await client.GetStringAsync("/Admin/Dashboard/Index?culture=en");
        Assert.Contains("/Admin/Accounts/Register", adminLayout);
        Assert.Contains("/Admin/Categories/Categories", adminLayout);
        Assert.Contains("/Admin/Reports/Index", adminLayout);

        var publicLayout = await client.GetStringAsync("/?culture=en");
        var navMatch = Regex.Match(
            publicLayout,
            "<nav[^>]*class=\"main-nav\"[^>]*>[\\s\\S]*?</nav>",
            RegexOptions.IgnoreCase);
        Assert.True(navMatch.Success, "Public main navigation not found");

        var nav = navMatch.Value;
        Assert.DoesNotContain("/Admin/Dashboard", nav);
        Assert.DoesNotContain("/Admin/Categories", nav);
        Assert.DoesNotContain("/Admin/Accounts/Register", nav);

        // The home page feature cards currently do expose admin links to everyone.
        Assert.Contains("/Admin/Categories", publicLayout);
    }

    [Fact]
    public async Task NormalUser_RegisterPageCurrentlyOffersAdminRole()
    {
        var email = await SeedUserAsync("User");
        var client = await CreateLoggedInClientAsync(email);

        var html = await client.GetStringAsync("/Admin/Accounts/Register?culture=en");

        Assert.Contains("id=\"ddluserRole\"", html);
        Assert.Contains("value=\"Admin\"", html);
    }

    [Fact]
    public async Task NormalUser_CanCurrentlyReachChangePasswordAction_WithoutTargetOwnershipCheck()
    {
        var email = await SeedUserAsync("User");
        var client = await CreateLoggedInClientAsync(email);
        var token = ExtractToken(
            await client.GetStringAsync("/Admin/Accounts/Register?culture=en"));

        // No password is supplied and the id does not exist, so this only proves the
        // action is reachable; it does not mutate any account.
        var response = await client.PostAsync("/Admin/Accounts/ChangePassword?culture=en",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["changePassword.Id"] = Guid.NewGuid().ToString(),
                ["changePassword.NewPassword"] = string.Empty,
                ["changePassword.ConfirmPassword"] = string.Empty,
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Accounts/Register", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AnonymousPublicLayout_CurrentlyExposesOpsLinks()
    {
        var client = CreateClient();

        var html = await client.GetStringAsync("/?culture=en");

        Assert.Contains("href=\"/hangfire\"", html);
        Assert.Contains("href=\"/swagger\"", html);
        Assert.Contains("href=\"/health\"", html);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<HttpClient> CreateLoggedInClientAsync(string email)
    {
        var client = CreateClient();
        var token = ExtractToken(await client.GetStringAsync("/Admin?culture=en"));
        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = Password,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Unexpected login status: {response.StatusCode}");
        return client;
    }

    private async Task<string> SeedUserAsync(string role)
    {
        var email = $"matrix-{Guid.NewGuid():N}@test.local";
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
            Name = $"Matrix {role}",
            ActiveUser = true,
            EmailConfirmed = true,
            ImageUser = string.Empty
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        return email;
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
