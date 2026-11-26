using System.Net;

namespace FreeBook.Tests;

public class PublicHomeEmptyStateTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicHomeEmptyStateTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("en", "No books are available right now.")]
    [InlineData("ar", "لا توجد كتب متاحة حاليًا.")]
    public async Task PublicHome_ShowsLocalizedEmptyState(string culture, string expectedText)
    {
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/?culture={culture}"));

        Assert.Contains("id=\"availableBooks\"", html);
        Assert.Contains("available-books-empty", html);
        Assert.Contains(expectedText, html);
    }
}
