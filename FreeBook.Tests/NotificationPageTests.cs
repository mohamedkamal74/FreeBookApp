using System.Net;
using System.Text.RegularExpressions;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class NotificationPageTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "notify-admin@bookify.local";
    private const string UserEmail = "notify-user@bookify.local";
    private const string Password = "Admin@12345";
    private readonly CustomWebApplicationFactory _factory;

    public NotificationPageTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_Request_Redirects_To_Login()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Admin/Notifications/Index");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Accounts/Login", response.Headers.Location?.OriginalString ?? string.Empty);
    }

    [Fact]
    public async Task Admin_Sees_Notification_And_Unread_Badge()
    {
        await SeedAdminAsync();
        await SeedNotificationAsync();
        var client = await CreateLoggedInClientAsync(AdminEmail);

        var html = await client.GetStringAsync("/Admin/Notifications/Index?culture=en");

        Assert.Contains("Ahmed Ali", html);
        Assert.Contains("Clean Code", html);
        Assert.Contains("navbar-badge", html);
        Assert.Matches("navbar-badge\">[1-9][0-9]*</span>", html);
        Assert.Contains("scope=\"col\"", html);
        Assert.Contains("Mark all as read", html);
    }

    [Fact]
    public async Task MarkAllRead_Clears_Unread_Badge()
    {
        await SeedAdminAsync();
        await SeedNotificationAsync();
        var client = await CreateLoggedInClientAsync(AdminEmail);

        var page = await client.GetStringAsync("/Admin/Notifications/Index?culture=en");
        var token = ExtractToken(page);
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        });
        var response = await client.PostAsync("/Admin/Notifications/MarkAllRead?culture=en", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var after = await client.GetStringAsync("/Admin/Notifications/Index?culture=en");
        Assert.DoesNotContain("navbar-badge", after);
        Assert.Contains("Clear read", after);
    }

    [Fact]
    public async Task User_Role_Is_Forbidden_From_Notification_Center()
    {
        await SeedUserAsync();
        var client = await CreateLoggedInClientAsync(UserEmail);

        var response = await client.GetAsync("/Admin/Notifications/Index?culture=en");

        // Cookie auth sends authenticated-but-unauthorized users to AccessDeniedPath.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Home/Denied", response.Headers.Location?.OriginalString ?? string.Empty);
    }

    private async Task SeedNotificationAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        await repo.UpsertAsync(new NotificationCommand
        {
            Type = NotificationKindDto.RentalOverdue,
            DedupeKey = $"overdue:{Guid.NewGuid()}:{Guid.NewGuid()}",
            SubscriberName = "Ahmed Ali",
            BookTitle = "Clean Code",
            Days = 4,
            TargetUrl = "/Admin/Rentals/Rentals"
        });
    }

    private async Task SeedAdminAsync() => await SeedUserAsync(AdminEmail, "Admin", "Administrator");

    private async Task SeedUserAsync() => await SeedUserAsync(UserEmail, "User", "Regular User");

    private async Task SeedUserAsync(string email, string role, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = name,
            ActiveUser = true,
            EmailConfirmed = true,
            ImageUser = string.Empty
        };
        var result = await userManager.CreateAsync(user, Password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, role);
    }

    private async Task<HttpClient> CreateLoggedInClientAsync(string email)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPage = await client.GetStringAsync("/Admin?culture=en");
        var token = ExtractToken(loginPage);
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = Password,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        });
        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Unexpected login status: {response.StatusCode}");
        return client;
    }

    private static string ExtractToken(string html)
    {
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase).Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }
}
