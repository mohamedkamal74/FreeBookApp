using System.Net;
using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class PublicHomeCardsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicHomeCardsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicHome_ShowsAvailableBookCardsForAnonymousVisitors()
    {
        await SeedBooksAsync();
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/?culture=en"));

        Assert.Contains("id=\"availableBooks\"", html);
        Assert.Contains("Available books", html);
        Assert.Contains("Public Home Book", html);
        Assert.Contains("Public Home Author", html);
        Assert.Contains("Public Home Category", html);
        Assert.Contains("https://res.cloudinary.com/example/image/upload/public-home-thumb.png", html);
        Assert.Contains("available-book-badge", html);
        Assert.Contains("Available", html);
        Assert.DoesNotContain("Hidden Book", html);
        Assert.DoesNotContain("Deleted Book", html);
    }

    [Fact]
    public void PublicHomeDtos_DoNotExposeEntityTypes()
    {
        var entityTypes = typeof(Book).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "Domin.Entity")
            .ToHashSet();
        var dtoTypes = new[] { typeof(AvailableBookCardDto), typeof(PublicHomeViewModel) };

        foreach (var dtoType in dtoTypes)
        {
            foreach (var property in dtoType.GetProperties())
            {
                var propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                Assert.DoesNotContain(propertyType, entityTypes);
            }
        }
    }

    [Fact]
    public void PublicHomeQueries_TranslateToSqlServer()
    {
        using var context = new FreeBookDbContext(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseSqlServer("Server=localhost;Database=FreeBookPublicHomeQuery;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options);

        var sql = PublicHomeQueries.AvailableBooks(context).Take(12).ToQueryString();

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IsAvailableForRental", sql);
    }

    private async Task SeedBooksAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FreeBookDbContext>();

        var author = new Author { Id = Guid.NewGuid(), Name = "Public Home Author" };
        var category = new Category { Id = Guid.NewGuid(), Name = "Public Home Category" };
        context.Authors.Add(author);
        context.categories.Add(category);

        context.books.AddRange(
            new Book
            {
                Id = Guid.NewGuid(),
                Name = "Public Home Book",
                AuthorId = author.Id,
                CategoryId = category.Id,
                ImageThumbnailUrl = "https://res.cloudinary.com/example/image/upload/public-home-thumb.png",
                IsAvailableForRental = true
            },
            new Book
            {
                Id = Guid.NewGuid(),
                Name = "Hidden Book",
                AuthorId = author.Id,
                CategoryId = category.Id,
                IsAvailableForRental = false
            },
            new Book
            {
                Id = Guid.NewGuid(),
                Name = "Deleted Book",
                AuthorId = author.Id,
                CategoryId = category.Id,
                IsAvailableForRental = true,
                CurrentState = 0
            });

        await context.SaveChangesAsync();
    }
}
