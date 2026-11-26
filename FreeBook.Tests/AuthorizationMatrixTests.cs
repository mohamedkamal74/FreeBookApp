using System.Net;
using System.Text.RegularExpressions;
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

    private static readonly string[] AdminOnlyUrls =
    {
        "/Admin/Dashboard/Index?culture=en",
        "/Admin/BooksDashboard/Index?culture=en",
        "/Admin/SubscribersDashboard/Index?culture=en",
        "/Admin/Categories/Categories?culture=en",
        "/Admin/Accounts/Register?culture=en",
        "/Admin/Accounts/Roles?culture=en",
        "/Admin/Reports/Books?culture=en",
        "/Admin/Books/Books?culture=en",
        "/Admin/Authors/Authors?culture=en",
        "/Admin/Subscribers/Subscribers?culture=en",
        "/Admin/Rentals/Rentals?culture=en",
        "/Admin/Notifications/Index?culture=en"
    };

    private static readonly string[] PublicSurfaceAdminLinks =
    {
        "/Admin/Categories/Categories",
        "/Admin/Authors/Authors",
        "/Admin/BookCopies/BookCopies",
        "/Admin/Subscribers/Subscribers",
        "/Admin/Reports/Index",
        "/Admin/Reports/OverdueBooks",
        "/Admin/Reports/PopularBooks",
        "/Admin/Reports/ActiveSubscribers",
        "/Admin/Reports/ExpiringSubscriptions",
        "/Admin/Reports/RentalHistory",
        "/Admin/Reports/BooksByCategory",
        "/Admin/Reports/BooksByAuthor"
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
    public async Task NormalUser_IsDeniedFromAdminAreaPages()
    {
        var email = await SeedUserAsync("User");
        var client = await CreateLoggedInClientAsync(email);

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

        foreach (var url in AdminOnlyUrls)
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task AdminLayout_ShowsAdminLinks_ButPublicPagesHideThemForNormalUser()
    {
        var adminEmail = await SeedUserAsync("Admin");
        var adminClient = await CreateLoggedInClientAsync(adminEmail);

        var adminLayout = await adminClient.GetStringAsync("/Admin/Accounts/Register?culture=en");
        Assert.Contains("/Admin/Accounts/Register", adminLayout);
        Assert.Contains("/Admin/Categories/Categories", adminLayout);
        Assert.Contains("/Admin/Reports/Index", adminLayout);

        var userEmail = await SeedUserAsync("User");
        var userClient = await CreateLoggedInClientAsync(userEmail);

        var publicLayout = await userClient.GetStringAsync("/?culture=en");
        var navMatch = Regex.Match(
            publicLayout,
            "<nav[^>]*class=\"main-nav\"[^>]*>[\\s\\S]*?</nav>",
            RegexOptions.IgnoreCase);
        Assert.True(navMatch.Success, "Public main navigation not found");

        var nav = navMatch.Value;
        Assert.DoesNotContain("/Admin/Dashboard", nav);
        Assert.DoesNotContain("/Admin/Categories", nav);
        Assert.DoesNotContain("/Admin/Accounts/Register", nav);

        AssertPublicAdminLinks(publicLayout, expected: false);
    }

    [Fact]
    public async Task RegisterPage_OffersRoleDropdownToAdminOnly()
    {
        var adminEmail = await SeedUserAsync("Admin");
        var adminClient = await CreateLoggedInClientAsync(adminEmail);

        var html = await adminClient.GetStringAsync("/Admin/Accounts/Register?culture=en");

        Assert.Contains("id=\"ddluserRole\"", html);
        Assert.Contains("value=\"Admin\"", html);
    }

    [Fact]
    public async Task AnonymousPublicLayout_HidesOpsLinksButKeepsHealth()
    {
        var client = CreateClient();

        var html = await client.GetStringAsync("/?culture=en");

        Assert.DoesNotContain("href=\"/hangfire\"", html);
        Assert.DoesNotContain("href=\"/swagger\"", html);
        Assert.Contains("href=\"/health\"", html);
    }

    [Fact]
    public async Task NormalUserPublicLayout_HidesOpsLinksButKeepsHealth()
    {
        var email = await SeedUserAsync("User");
        var client = await CreateLoggedInClientAsync(email);

        var html = await client.GetStringAsync("/?culture=en");

        Assert.DoesNotContain("href=\"/hangfire\"", html);
        Assert.DoesNotContain("href=\"/swagger\"", html);
        Assert.Contains("href=\"/health\"", html);
    }

    [Fact]
    public async Task AdminPublicLayout_ShowsOpsLinksInDevelopment()
    {
        var email = await SeedUserAsync("Admin");
        var client = await CreateLoggedInClientAsync(email);

        var html = await client.GetStringAsync("/?culture=en");

        Assert.Contains("href=\"/hangfire\"", html);
        Assert.Contains("href=\"/swagger\"", html);
        Assert.Contains("href=\"/health\"", html);
    }

    [Fact]
    public async Task AnonymousPublicHome_HidesAllAdminLinks()
    {
        var html = await CreateClient().GetStringAsync("/?culture=en");

        AssertPublicAdminLinks(html, expected: false);
    }

    [Fact]
    public async Task NormalUserPublicHome_HidesAllAdminLinks()
    {
        var email = await SeedUserAsync("User");
        var client = await CreateLoggedInClientAsync(email);

        var html = await client.GetStringAsync("/?culture=en");

        AssertPublicAdminLinks(html, expected: false);
        Assert.Contains("/Admin/Accounts/ChangePassword", html);
    }

    [Fact]
    public async Task AdminPublicHome_ShowsAdminLinks()
    {
        var email = await SeedUserAsync("Admin");
        var client = await CreateLoggedInClientAsync(email);

        var html = await client.GetStringAsync("/?culture=en");

        AssertPublicAdminLinks(html, expected: true);
    }

    [Fact]
    public async Task AdminLayoutSidebar_IsAdminOnly()
    {
        var adminEmail = await SeedUserAsync("Admin");
        var adminClient = await CreateLoggedInClientAsync(adminEmail);

        var adminHtml = await adminClient.GetStringAsync("/Admin/Home/Denied?culture=en");
        Assert.Contains("class=\"mt-2 mr-6\"", adminHtml);
        Assert.Contains("/Admin/Categories/Categories", adminHtml);

        var userEmail = await SeedUserAsync("User");
        var userClient = await CreateLoggedInClientAsync(userEmail);

        var userHtml = await userClient.GetStringAsync("/Admin/Home/Denied?culture=en");
        Assert.DoesNotContain("class=\"mt-2 mr-6\"", userHtml);
        Assert.DoesNotContain("/Admin/Categories/Categories", userHtml);
        Assert.Contains("/Admin/Accounts/ChangePassword", userHtml);
    }

    private static void AssertPublicAdminLinks(string html, bool expected)
    {
        foreach (var url in PublicSurfaceAdminLinks)
        {
            if (expected)
            {
                Assert.Contains(url, html);
            }
            else
            {
                Assert.DoesNotContain(url, html);
            }
        }
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
