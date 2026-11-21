using Infrastructure.Common.Configuration;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FreeBook.Tests;

public class ImageServiceTests
{
    [Fact]
    public async Task UploadAsync_WithoutCredentials_ReturnsFailureInsteadOfThrowing()
    {
        var service = new CloudinaryImageService(
            Options.Create(new CloudinarySettings()),
            Mock.Of<ILogger<CloudinaryImageService>>());
        var content = new byte[] { 1, 2, 3, 4 };
        var file = new FormFile(new MemoryStream(content), 0, content.Length, "file", "cover.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await service.UploadAsync(file);

        Assert.False(result.Success);
        Assert.Equal("Cloudinary is not configured", result.Error);
    }
}
