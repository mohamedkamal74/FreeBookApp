using System.Net;
using System.Text.RegularExpressions;
using Domin.Entity;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class BooksDashboardReportTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "books-admin@test.local";
    private const string UserEmail = "books-user@test.local";
    private const string Password = "Admin@12345";

    private readonly CustomWebApplicationFactory _factory;

    public BooksDashboardReportTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BooksDashboardAndReport_LoadForAdmin_WithLocalizedHooks()
    {
        await SeedUserAsync(AdminEmail, "Admin", "Books Administrator");
        var (_, bookName) = await SeedBookWithCopiesAsync();
        var client = CreateClient();
        await LoginAsync(client, AdminEmail);

        var dashboard = await client.GetStringAsync("/Admin/BooksDashboard/Index?culture=en");
        Assert.Contains("Books Dashboard", dashboard);
        Assert.Contains("Total Copies", dashboard);
        Assert.Contains("Rented Copies", dashboard);
        Assert.Contains("Books Without Copies", dashboard);
        Assert.Contains("\"totalCopies\":", dashboard);
        Assert.Contains("id=\"chartBookAvailability\"", dashboard);
        Assert.Contains("id=\"chartBooksByCategory\"", dashboard);
        Assert.Contains("id=\"chartBooksByAuthor\"", dashboard);
        Assert.Contains("id=\"chartTopRentedBooks\"", dashboard);
        Assert.Contains("id=\"chartRentalsPerMonth\"", dashboard);
        Assert.Contains("id=\"filterFrom\"", dashboard);
        Assert.Contains("id=\"filterAuthor\"", dashboard);
        Assert.Contains("id=\"filterCategory\"", dashboard);

        var report = await client.GetStringAsync("/Admin/Reports/Books?culture=en");
        Assert.Contains("Books Report", report);
        Assert.Contains("id=\"tableBooksReport\"", report);
        Assert.Contains("initReportTable('#tableBooksReport'", report);
        Assert.Contains("no-export", report);
        Assert.Contains("Availability", report);
        Assert.Contains(bookName, report);
        Assert.Contains("id=\"reportAuthor\"", report);
        Assert.Contains("id=\"reportCategory\"", report);
        Assert.Contains("id=\"reportAvailableOnly\"", report);

        var row = Regex.Matches(report, @"<tr\b[^>]*>[\s\S]*?</tr>", RegexOptions.IgnoreCase)
            .FirstOrDefault(match => match.Value.Contains(bookName, StringComparison.Ordinal));
        Assert.NotNull(row);
        var cells = Regex.Matches(row.Value, @"<td[^>]*>([\s\S]*?)</td>", RegexOptions.IgnoreCase)
            .Select(match => Regex.Replace(match.Groups[1].Value, "<[^>]+>", string.Empty).Trim())
            .ToList();
        Assert.Equal(11, cells.Count);
        Assert.Equal("2", cells[7]);
        Assert.Equal("1", cells[8]);
        Assert.Equal("0", cells[9]);
        Assert.Equal("Available", cells[10]);

        var arabicDashboard = WebUtility.HtmlDecode(
            await client.GetStringAsync("/Admin/BooksDashboard/Index?culture=ar"));
        Assert.Contains("lang=\"ar\"", arabicDashboard);
        Assert.Contains("dir=\"rtl\"", arabicDashboard);
        Assert.Contains("لوحة الكتب", arabicDashboard);

        var arabicReport = WebUtility.HtmlDecode(
            await client.GetStringAsync("/Admin/Reports/Books?culture=ar"));
        Assert.Contains("dir=\"rtl\"", arabicReport);
        Assert.Contains("تقرير الكتب", arabicReport);
        Assert.Contains("التوفر", arabicReport);
    }

    [Fact]
    public async Task BooksDashboard_AllowsUserRole_ButReportDeniesIt()
    {
        await SeedUserAsync(UserEmail, "User", "Regular User");
        var client = CreateClient();
        await LoginAsync(client, UserEmail);

        var dashboard = await client.GetAsync("/Admin/BooksDashboard/Index?culture=en");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);

        var report = await client.GetAsync("/Admin/Reports/Books?culture=en");
        Assert.Equal(HttpStatusCode.Redirect, report.StatusCode);
        Assert.Contains("/Admin/Home/Denied", report.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AnonymousBooksDashboardAndReport_RedirectToLogin()
    {
        var client = CreateClient();

        foreach (var url in new[] { "/Admin/BooksDashboard/Index", "/Admin/Reports/Books" })
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Admin", response.Headers.Location!.OriginalString);
        }
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<(Guid Id, string Name)> SeedBookWithCopiesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var authors = scope.ServiceProvider.GetRequiredService<IServiceRepository<Author>>();
        var books = scope.ServiceProvider.GetRequiredService<IServiceRepository<Book>>();
        var copies = scope.ServiceProvider.GetRequiredService<IServiceRepository<BookCopy>>();

        var author = new Author { Name = $"Books Dashboard Author {Guid.NewGuid():N}" };
        Assert.True((await authors.SaveAsync(author)).IsSuccess);

        var book = new Book
        {
            Name = $"Books Dashboard Book {Guid.NewGuid():N}",
            AuthorId = author.Id,
            Publisher = "Test Press",
            Hall = "A1",
            IsAvailableForRental = true
        };
        Assert.True((await books.SaveAsync(book)).IsSuccess);

        Assert.True((await copies.SaveAsync(new BookCopy
        {
            BookId = book.Id,
            EditionNumber = 1,
            SerialNumber = 1,
            IsAvailableForRental = true
        })).IsSuccess);
        Assert.True((await copies.SaveAsync(new BookCopy
        {
            BookId = book.Id,
            EditionNumber = 2,
            SerialNumber = 2,
            IsAvailableForRental = false
        })).IsSuccess);

        return (book.Id, book.Name);
    }

    private async Task SeedUserAsync(string email, string role, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

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
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        await userManager.AddToRoleAsync(user, role);
    }

    private async Task LoginAsync(HttpClient client, string email)
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
                ["Email"] = email,
                ["Password"] = Password,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Unexpected login status: {response.StatusCode}");
    }
}
