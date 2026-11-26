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
        var (_, bookName, subCategoryName) = await SeedBookWithCopiesAsync();
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
        Assert.Equal(subCategoryName, cells[4]);
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
    public async Task DashboardsAndReports_LoadForAdmin_WithLocalizedChartLabelsAndRtl()
    {
        await SeedUserAsync(AdminEmail, "Admin", "Books Administrator");
        var client = CreateClient();
        await LoginAsync(client, AdminEmail);

        var dashboardUrls = new[]
        {
            "/Admin/Dashboard/Index",
            "/Admin/SubscribersDashboard/Index",
            "/Admin/BooksDashboard/Index"
        };
        var reportUrls = new[]
        {
            "/Admin/Reports/Index",
            "/Admin/Reports/OverdueBooks",
            "/Admin/Reports/PopularBooks",
            "/Admin/Reports/ActiveSubscribers",
            "/Admin/Reports/ExpiringSubscriptions",
            "/Admin/Reports/RentalHistory",
            "/Admin/Reports/BooksByCategory",
            "/Admin/Reports/BooksByAuthor",
            "/Admin/Reports/Books"
        };

        foreach (var url in dashboardUrls.Concat(reportUrls))
        {
            var response = await client.GetAsync($"{url}?culture=en");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var generalDashboard = await client.GetStringAsync("/Admin/Dashboard/Index?culture=en");
        Assert.Contains("id=\"chartRentalsPerMonth\"", generalDashboard);
        Assert.Contains("id=\"chartBooksByCategory\"", generalDashboard);
        Assert.Contains("id=\"chartCopyAvailability\"", generalDashboard);
        Assert.Contains("id=\"chartTopRentedBooks\"", generalDashboard);
        Assert.Contains("\"availableCopies\":", generalDashboard);

        var subscribersDashboard = await client.GetStringAsync("/Admin/SubscribersDashboard/Index?culture=en");
        Assert.Contains("id=\"chartByGovernorate\"", subscribersDashboard);
        Assert.Contains("id=\"chartAgeGroups\"", subscribersDashboard);
        Assert.Contains("id=\"chartSubscriptionsPerMonth\"", subscribersDashboard);
        Assert.Contains("id=\"chartSubscriptionStatus\"", subscribersDashboard);
        Assert.Contains("Under 18", subscribersDashboard);
        Assert.Contains("Active", subscribersDashboard);
        Assert.DoesNotContain("lbAgeUnder18", subscribersDashboard);
        Assert.DoesNotContain("lbSubscriptionStatus", subscribersDashboard);

        var booksDashboard = await client.GetStringAsync("/Admin/BooksDashboard/Index?culture=en");
        Assert.DoesNotContain("\"lbAvailable\"", booksDashboard);
        Assert.DoesNotContain("\"lbRented\"", booksDashboard);

        foreach (var url in dashboardUrls.Concat(reportUrls))
        {
            var arabic = WebUtility.HtmlDecode(await client.GetStringAsync($"{url}?culture=ar"));
            Assert.Contains("dir=\"rtl\"", arabic);
            Assert.DoesNotContain("lbAgeUnder18", arabic);
            Assert.DoesNotContain("\"lbAvailable\"", arabic);
        }
    }

    [Fact]
    public async Task BooksDashboard_AndReport_DenyUserRole()
    {
        await SeedUserAsync(UserEmail, "User", "Regular User");
        var client = CreateClient();
        await LoginAsync(client, UserEmail);

        foreach (var url in new[] { "/Admin/BooksDashboard/Index?culture=en", "/Admin/Reports/Books?culture=en" })
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Admin/Home/Denied", response.Headers.Location!.OriginalString);
        }
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

    private async Task<(Guid Id, string Name, string SubCategoryName)> SeedBookWithCopiesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var authors = scope.ServiceProvider.GetRequiredService<IServiceRepository<AuthorDto, AuthorCommand>>();
        var categories = scope.ServiceProvider.GetRequiredService<IServiceRepository<CategoryDto, CategoryCommand>>();
        var subCategories = scope.ServiceProvider.GetRequiredService<IServiceRepository<SubCategoryDto, SubCategoryCommand>>();
        var books = scope.ServiceProvider.GetRequiredService<IServiceRepository<BookDto, BookCommand>>();
        var copies = scope.ServiceProvider.GetRequiredService<IServiceRepository<BookCopyDto, BookCopyCommand>>();

        var authorName = $"Books Dashboard Author {Guid.NewGuid():N}";
        Assert.True((await authors.SaveAsync(new AuthorCommand { Name = authorName })).IsSuccess);
        var authorId = (await authors.FindByAsync(authorName)).Value!.Id;

        var categoryName = $"Books Dashboard Category {Guid.NewGuid():N}";
        Assert.True((await categories.SaveAsync(new CategoryCommand { Name = categoryName })).IsSuccess);
        var categoryId = (await categories.FindByAsync(categoryName)).Value!.Id;

        var subCategoryName = $"Books Dashboard Subcategory {Guid.NewGuid():N}";
        Assert.True((await subCategories.SaveAsync(new SubCategoryCommand
        {
            Name = subCategoryName,
            CategoryId = categoryId
        })).IsSuccess);
        var subCategoryId = (await subCategories.FindByAsync(subCategoryName)).Value!.Id;

        var bookName = $"Books Dashboard Book {Guid.NewGuid():N}";
        Assert.True((await books.SaveAsync(new BookCommand
        {
            Name = bookName,
            AuthorId = authorId,
            CategoryId = categoryId,
            SubCategoryId = subCategoryId,
            Publisher = "Test Press",
            Hall = "A1",
            IsAvailableForRental = true
        })).IsSuccess);
        var book = (await books.FindByAsync(bookName)).Value!;

        Assert.True((await copies.SaveAsync(new BookCopyCommand
        {
            BookId = book.Id,
            EditionNumber = 1,
            SerialNumber = 1,
            IsAvailableForRental = true
        })).IsSuccess);
        Assert.True((await copies.SaveAsync(new BookCopyCommand
        {
            BookId = book.Id,
            EditionNumber = 2,
            SerialNumber = 2,
            IsAvailableForRental = false
        })).IsSuccess);

        return (book.Id, book.Name, subCategoryName);
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
