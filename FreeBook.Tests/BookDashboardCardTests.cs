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

public class BookDashboardCardTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "book-cards@test.local";
    private const string Password = "Admin@12345";

    private readonly CustomWebApplicationFactory _factory;

    public BookDashboardCardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BookAndCopyCards_MatchFilteredListCounts()
    {
        var (authorId, categoryId) = await SeedAsync();
        var client = CreateClient();
        await LoginAsync(client);

        var general = Decode(await client.GetStringAsync("/Admin/Dashboard/Index?culture=en"));
        AssertCard(general, "Total Books", "4");
        AssertCard(general, "Available Copies", "2");

        var totalBooksHref = GetCardHref(general, "Total Books", "4");
        Assert.Contains("/Admin/Books/Books", totalBooksHref, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Admin/Categories", totalBooksHref, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "filter=available",
            GetCardHref(general, "Available Copies", "2"),
            StringComparison.OrdinalIgnoreCase);

        var books = Decode(await client.GetStringAsync("/Admin/Books/Books?culture=en"));
        Assert.Equal(4, CountRows(GetTableBody(books, "tableBooks")));

        var generalAvailable = Decode(await client.GetStringAsync(
            "/Admin/BookCopies/BookCopies?culture=en&filter=available"));
        Assert.Equal(2, CountRows(GetTableBody(generalAvailable, "tableBookCopies")));

        var dashboard = Decode(await client.GetStringAsync(
            $"/Admin/BooksDashboard/Index?culture=en&authorId={authorId}&categoryId={categoryId}"));
        AssertCard(dashboard, "Total Books", "2");
        AssertCard(dashboard, "Total Copies", "2");
        AssertCard(dashboard, "Available Copies", "1");
        AssertCard(dashboard, "Rented Copies", "1");
        AssertCard(dashboard, "Books Without Copies", "1");

        var authorRoute = $"authorId={authorId}";
        var categoryRoute = $"categoryId={categoryId}";
        AssertCardLink(dashboard, "Total Books", "2", "/Admin/Books/Books", authorRoute, categoryRoute);
        AssertCardLink(dashboard, "Total Copies", "2", "/Admin/BookCopies/BookCopies", authorRoute, categoryRoute);
        AssertCardLink(
            dashboard,
            "Available Copies",
            "1",
            "/Admin/BookCopies/BookCopies",
            authorRoute,
            categoryRoute,
            "filter=available");
        AssertCardLink(
            dashboard,
            "Rented Copies",
            "1",
            "/Admin/BookCopies/BookCopies",
            authorRoute,
            categoryRoute,
            "filter=rented");
        Assert.DoesNotContain(
            "/Admin/Rentals",
            GetCardHref(dashboard, "Rented Copies", "1"),
            StringComparison.OrdinalIgnoreCase);
        AssertCardLink(
            dashboard,
            "Books Without Copies",
            "1",
            "/Admin/Books/Books",
            authorRoute,
            categoryRoute,
            "filter=without-copies");

        var scopedBooks = Decode(await client.GetStringAsync(
            $"/Admin/Books/Books?culture=en&{authorRoute}&{categoryRoute}"));
        Assert.Equal(2, CountRows(GetTableBody(scopedBooks, "tableBooks")));

        var scopedCopies = Decode(await client.GetStringAsync(
            $"/Admin/BookCopies/BookCopies?culture=en&{authorRoute}&{categoryRoute}"));
        Assert.Equal(2, CountRows(GetTableBody(scopedCopies, "tableBookCopies")));

        var scopedAvailable = Decode(await client.GetStringAsync(
            $"/Admin/BookCopies/BookCopies?culture=en&{authorRoute}&{categoryRoute}&filter=available"));
        Assert.Equal(1, CountRows(GetTableBody(scopedAvailable, "tableBookCopies")));

        var scopedRented = Decode(await client.GetStringAsync(
            $"/Admin/BookCopies/BookCopies?culture=en&{authorRoute}&{categoryRoute}&filter=rented"));
        Assert.Equal(1, CountRows(GetTableBody(scopedRented, "tableBookCopies")));

        var scopedWithoutCopies = Decode(await client.GetStringAsync(
            $"/Admin/Books/Books?culture=en&{authorRoute}&{categoryRoute}&filter=without-copies"));
        var withoutCopiesBody = GetTableBody(scopedWithoutCopies, "tableBooks");
        Assert.Equal(1, CountRows(withoutCopiesBody));
        Assert.Contains("Scoped Book Without Copies", withoutCopiesBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Scoped Book With Copies", withoutCopiesBody, StringComparison.OrdinalIgnoreCase);

        var arabicGeneral = Decode(await client.GetStringAsync("/Admin/Dashboard/Index?culture=ar"));
        Assert.Contains("dir=\"rtl\"", arabicGeneral);
        Assert.DoesNotContain("lbTotalBooks", arabicGeneral);

        var arabicDashboard = Decode(await client.GetStringAsync(
            $"/Admin/BooksDashboard/Index?culture=ar&{authorRoute}&{categoryRoute}"));
        Assert.Contains("dir=\"rtl\"", arabicDashboard);
        Assert.DoesNotContain("lbTotalCopies", arabicDashboard);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<(Guid AuthorId, Guid CategoryId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<FreeBookDbContext>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var user = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                Name = "Book Cards Admin",
                ActiveUser = true,
                EmailConfirmed = true,
                ImageUser = string.Empty
            };
            var created = await userManager.CreateAsync(user, Password);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
            Assert.True((await userManager.AddToRoleAsync(user, "Admin")).Succeeded);
        }

        var authorId = Guid.NewGuid();
        var otherAuthorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();

        var scopedWithCopies = CreateBook("Scoped Book With Copies", authorId, categoryId, active: true);
        var scopedWithoutCopies = CreateBook("Scoped Book Without Copies", authorId, categoryId, active: true);
        var otherAuthor = CreateBook("Other Author Book", otherAuthorId, categoryId, active: true);
        var otherCategory = CreateBook("Other Category Book", authorId, otherCategoryId, active: true);
        var deleted = CreateBook("Deleted Book With Copies", authorId, categoryId, active: false);

        context.AddRange(
            new Author { Id = authorId, Name = "Book Cards Author" },
            new Author { Id = otherAuthorId, Name = "Book Cards Other Author" },
            new Category { Id = categoryId, Name = "Book Cards Category" },
            new Category { Id = otherCategoryId, Name = "Book Cards Other Category" },
            scopedWithCopies,
            scopedWithoutCopies,
            otherAuthor,
            otherCategory,
            deleted,
            CreateCopy(scopedWithCopies.Id, 1, available: true),
            CreateCopy(scopedWithCopies.Id, 2, available: false),
            CreateCopy(otherAuthor.Id, 1, available: true),
            CreateCopy(otherAuthor.Id, 2, available: false),
            CreateCopy(otherCategory.Id, 1, available: false),
            CreateCopy(deleted.Id, 1, available: true),
            CreateCopy(deleted.Id, 2, available: false));
        await context.SaveChangesAsync();

        return (authorId, categoryId);
    }

    private static Book CreateBook(string name, Guid authorId, Guid categoryId, bool active) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        AuthorId = authorId,
        CategoryId = categoryId,
        IsAvailableForRental = true,
        CurrentState = (int)(active
            ? Domin.Entity.Helper.ECurrentState.Active
            : Domin.Entity.Helper.ECurrentState.Delete)
    };

    private static BookCopy CreateCopy(Guid bookId, int serialNumber, bool available) => new()
    {
        Id = Guid.NewGuid(),
        BookId = bookId,
        EditionNumber = 1,
        SerialNumber = serialNumber,
        IsAvailableForRental = available
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

    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    private static void AssertCard(string html, string label, string value)
    {
        var pattern = $@"<h3>\s*{Regex.Escape(value)}\s*</h3>\s*<p>\s*{Regex.Escape(label)}\s*</p>";
        Assert.True(
            Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase),
            $"Card {label} with value {value} was not rendered");
    }

    private static void AssertCardLink(
        string html,
        string label,
        string value,
        params string[] expectedFragments)
    {
        var href = GetCardHref(html, label, value);
        foreach (var fragment in expectedFragments)
        {
            Assert.Contains(fragment, href, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string GetCardHref(string html, string label, string value)
    {
        var cardPattern = $@"<h3>\s*{Regex.Escape(value)}\s*</h3>\s*<p>\s*{Regex.Escape(label)}\s*</p>";
        foreach (Match anchor in Regex.Matches(html, "<a\\b[^>]*>[\\s\\S]*?</a>", RegexOptions.IgnoreCase))
        {
            if (!Regex.IsMatch(anchor.Value, cardPattern, RegexOptions.IgnoreCase))
            {
                continue;
            }

            var href = Regex.Match(anchor.Value, "href=\"([^\"]*)\"", RegexOptions.IgnoreCase);
            Assert.True(href.Success, $"Card {label} has no href");
            return Decode(href.Groups[1].Value);
        }

        Assert.Fail($"Card {label} with value {value} was not rendered as a link");
        return string.Empty;
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
