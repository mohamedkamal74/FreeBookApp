using System.Net;
using System.Text.RegularExpressions;
using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class DashboardCardCountTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "dashboard-cards@test.local";
    private const string Password = "Admin@12345";

    private readonly CustomWebApplicationFactory _factory;

    public DashboardCardCountTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DashboardCards_MatchFilteredListCounts()
    {
        await SeedUserAsync();
        await SeedDashboardDataAsync();

        var client = CreateClient();
        await LoginAsync(client);

        var general = await client.GetStringAsync("/Admin/Dashboard/Index?culture=en");
        AssertCard(general, "Total Books", "1");
        AssertCard(general, "Active Subscriptions", "2");
        AssertCard(general, "Expiring Subscriptions", "1");
        AssertCard(general, "Blacklisted Subscribers", "1");
        AssertCardLink(general, "Subscriptions", "active");
        AssertCardLink(general, "Subscriptions", "expiring");
        AssertCardLink(general, "Subscribers", "blacklisted");

        var subscribersDashboard = await client.GetStringAsync("/Admin/SubscribersDashboard/Index?culture=en");
        AssertCard(subscribersDashboard, "Total Subscribers", "4");
        AssertCard(subscribersDashboard, "Active Subscriptions", "2");
        AssertCard(subscribersDashboard, "Expiring Subscriptions", "1");
        AssertCard(subscribersDashboard, "Blacklisted Subscribers", "1");
        AssertCardLink(subscribersDashboard, "Subscriptions", "active");

        var subscribers = await client.GetStringAsync("/Admin/Subscribers/Subscribers?culture=en&filter=blacklisted");
        var subscribersBody = GetTableBody(subscribers, "tableSubscribers");
        Assert.Equal(1, CountRows(subscribersBody));
        Assert.Contains("ZBlacklisted", subscribersBody);
        Assert.DoesNotContain("AActive", subscribersBody);
        Assert.DoesNotContain("BExpiring", subscribersBody);
        Assert.DoesNotContain("CExpired", subscribersBody);

        var active = await client.GetStringAsync("/Admin/Subscriptions/Subscriptions?culture=en&filter=active");
        var activeBody = GetTableBody(active, "tableSubscriptions");
        Assert.Equal(2, CountRows(activeBody));
        Assert.Contains("AActive", activeBody);
        Assert.Contains("BExpiring", activeBody);
        Assert.DoesNotContain("ZBlacklisted", activeBody);
        Assert.DoesNotContain("CExpired", activeBody);

        var expiring = await client.GetStringAsync("/Admin/Subscriptions/Subscriptions?culture=en&filter=expiring");
        var expiringBody = GetTableBody(expiring, "tableSubscriptions");
        Assert.Equal(1, CountRows(expiringBody));
        Assert.Contains("BExpiring", expiringBody);
        Assert.DoesNotContain("AActive", expiringBody);
        Assert.DoesNotContain("ZBlacklisted", expiringBody);
        Assert.DoesNotContain("CExpired", expiringBody);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task SeedUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (await userManager.FindByEmailAsync(AdminEmail) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            Name = "Dashboard Cards Admin",
            ActiveUser = true,
            EmailConfirmed = true,
            ImageUser = string.Empty
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
        Assert.True((await userManager.AddToRoleAsync(user, "Admin")).Succeeded);
    }

    private async Task SeedDashboardDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FreeBookDbContext>();
        var now = DateTime.UtcNow;

        var governorate = new Governorate
        {
            Id = Guid.NewGuid(),
            Name = "Card Governorate"
        };
        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = "Card Area",
            GovernorateId = governorate.Id
        };
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Card Count Book",
            AuthorId = Guid.NewGuid(),
            IsAvailableForRental = true
        };

        var blacklisted = CreateSubscriber("ZBlacklisted", "Card", now, governorate.Id, area.Id, blacklisted: true);
        var active = CreateSubscriber("AActive", "Card", now, governorate.Id, area.Id, blacklisted: false);
        var expiring = CreateSubscriber("BExpiring", "Card", now, governorate.Id, area.Id, blacklisted: false);
        var expired = CreateSubscriber("CExpired", "Card", now, governorate.Id, area.Id, blacklisted: false);

        context.AddRange(governorate, area, book, blacklisted, active, expiring, expired);
        context.AddRange(
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = blacklisted.Id,
                StartDate = now.AddDays(-100),
                EndDate = now.AddDays(-1)
            },
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = active.Id,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(100)
            },
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = expiring.Id,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(10)
            },
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = expired.Id,
                StartDate = now.AddDays(-200),
                EndDate = now.AddDays(-50)
            });
        await context.SaveChangesAsync();
    }

    private static Subscriber CreateSubscriber(
        string firstName,
        string lastName,
        DateTime now,
        Guid governorateId,
        Guid areaId,
        bool blacklisted) => new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = now.AddYears(-30),
            NationalId = Guid.NewGuid().ToString("N")[..11],
            MobileNumber = "010" + Guid.NewGuid().ToString("N")[..8],
            Email = $"{firstName.ToLowerInvariant()}@cards.test.local",
            GovernorateId = governorateId,
            AreaId = areaId,
            IsBlackListed = blacklisted,
            CreatedOn = now.AddDays(-5)
        };

    private async Task LoginAsync(HttpClient client)
    {
        var loginPage = await client.GetAsync("/Admin?culture=en");
        var token = Regex.Match(
            await loginPage.Content.ReadAsStringAsync(),
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase).Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = AdminEmail,
                ["Password"] = Password,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Unexpected login status: {response.StatusCode}");
    }

    private static void AssertCard(string html, string label, string value)
    {
        var decoded = WebUtility.HtmlDecode(html);
        var pattern = $@"<h3>\s*{Regex.Escape(value)}\s*</h3>\s*<p>\s*{Regex.Escape(label)}\s*</p>";
        Assert.True(
            Regex.IsMatch(decoded, pattern, RegexOptions.IgnoreCase),
            $"Card {label} with value {value} was not rendered");
    }

    private static void AssertCardLink(string html, string controller, string filter)
    {
        var pattern = $@"href=""[^""]*/Admin/{controller}[^""]*filter={filter}""";
        Assert.True(
            Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase),
            $"Link to /Admin/{controller}?filter={filter} was not rendered");
    }

    private static string GetTableBody(string html, string tableId)
    {
        var table = Regex.Match(
            html,
            $@"<table[^>]*id=""{tableId}""[^>]*>[\s\S]*?</table>",
            RegexOptions.IgnoreCase);
        Assert.True(table.Success, $"Table {tableId} was not rendered");

        var body = Regex.Match(table.Value, "<tbody[^>]*>([\\s\\S]*?)</tbody>", RegexOptions.IgnoreCase);
        Assert.True(body.Success, $"Table {tableId} body was not rendered");
        return body.Value;
    }

    private static int CountRows(string body) => Regex.Matches(body, "<tr\\b", RegexOptions.IgnoreCase).Count;
}
