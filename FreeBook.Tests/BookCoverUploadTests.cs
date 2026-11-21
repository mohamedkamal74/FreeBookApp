using System.Text;
using Domin.Entity;
using FreeBook.Areas.Admin.Controllers;
using FreeBook.Resources;
using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;

namespace FreeBook.Tests;

public class BookCoverUploadTests
{
    [Fact]
    public async Task Save_WithValidCover_UploadsImageAndStoresCloudinaryFields()
    {
        var books = new Mock<IServiceRepository<Book>>();
        var images = new Mock<IImageService>();
        Book? saved = null;

        books.Setup(x => x.FindByAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Success<Book?>(null));
        books.Setup(x => x.SaveAsync(It.IsAny<Book>()))
            .Callback<Book>(book => saved = book)
            .ReturnsAsync(Result.Success(true));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .ReturnsAsync(new ImageUploadResult
            {
                Success = true,
                PublicId = "bookify/books/new-cover",
                Url = "https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg",
                ThumbnailUrl = "https://res.cloudinary.com/demo/image/upload/c_fill,w_200,h_200/bookify/books/new-cover.jpg"
            });

        var (controller, session) = CreateController(books, images);
        var model = CreateModel();
        model.NewBook.CoverImage = CreateFile("cover.png");

        var result = await controller.Save(model);

        Assert.IsType<RedirectToActionResult>(result);
        images.Verify(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"), Times.Once);
        Assert.NotNull(saved);
        Assert.Equal("bookify/books/new-cover", saved!.ImagePublicId);
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg", saved.ImageUrl);
        Assert.Contains("c_fill,w_200,h_200", saved.ImageThumbnailUrl);
        Assert.Equal(Helper.Success, session.GetString(Helper.MsgType));
    }

    [Fact]
    public async Task Save_WithInvalidCoverExtension_DoesNotUploadOrSave()
    {
        var books = new Mock<IServiceRepository<Book>>();
        var images = new Mock<IImageService>();
        var (controller, session) = CreateController(books, images);
        var model = CreateModel();
        model.NewBook.CoverImage = CreateFile("cover.txt");

        var result = await controller.Save(model);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(Helper.Error, session.GetString(Helper.MsgType));
        Assert.Equal("msgCoverImageInvalid", session.GetString(Helper.Msg));
        images.Verify(x => x.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        images.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        books.Verify(x => x.SaveAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task Save_WhenCloudinaryUploadFails_DoesNotSaveBook()
    {
        var books = new Mock<IServiceRepository<Book>>();
        var images = new Mock<IImageService>();
        books.Setup(x => x.FindByAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Success<Book?>(null));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .ReturnsAsync(new ImageUploadResult { Success = false, Error = "Cloudinary unavailable" });

        var (controller, session) = CreateController(books, images);
        var model = CreateModel();
        model.NewBook.CoverImage = CreateFile("cover.png");

        var result = await controller.Save(model);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(Helper.Error, session.GetString(Helper.MsgType));
        Assert.Equal("msgCoverImageUploadFailed", session.GetString(Helper.Msg));
        books.Verify(x => x.SaveAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task Save_ReplaceCover_DeletesOldImageOnlyAfterSuccessfulSave()
    {
        var books = new Mock<IServiceRepository<Book>>();
        var images = new Mock<IImageService>();
        var events = new List<string>();
        var existing = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Clean Code",
            AuthorId = Guid.NewGuid(),
            ImagePublicId = "bookify/books/old-cover"
        };

        books.Setup(x => x.FindByAsync(existing.Name))
            .ReturnsAsync(Result.Success<Book?>(existing));
        books.Setup(x => x.FindByIdAsync(existing.Id))
            .ReturnsAsync(Result.Success<Book?>(existing));
        books.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result.Success(new List<Book> { existing }));
        books.Setup(x => x.SaveAsync(It.IsAny<Book>()))
            .Callback(() => events.Add("save"))
            .ReturnsAsync(Result.Success(true));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .Callback(() => events.Add("upload"))
            .ReturnsAsync(new ImageUploadResult
            {
                Success = true,
                PublicId = "bookify/books/new-cover",
                Url = "https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg",
                ThumbnailUrl = string.Empty
            });
        images.Setup(x => x.DeleteAsync("bookify/books/old-cover"))
            .Callback(() => events.Add("delete-old"))
            .Returns(Task.CompletedTask);

        var (controller, _) = CreateController(books, images);
        var model = CreateModel(existing.Name);
        model.NewBook.BookId = existing.Id.ToString();
        model.NewBook.AuthorId = existing.AuthorId;
        model.NewBook.CoverImage = CreateFile("new-cover.jpg");

        await controller.Save(model);

        Assert.Equal(new[] { "upload", "save", "delete-old" }, events);
        images.Verify(x => x.DeleteAsync("bookify/books/new-cover"), Times.Never);
    }

    [Fact]
    public async Task Save_ReplaceCover_KeepsOldImageWhenAnotherBookUsesIt()
    {
        var books = new Mock<IServiceRepository<Book>>();
        var images = new Mock<IImageService>();
        var existing = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Clean Code",
            AuthorId = Guid.NewGuid(),
            ImagePublicId = "bookify/books/shared-cover"
        };
        var other = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Clean Architecture",
            AuthorId = Guid.NewGuid(),
            ImagePublicId = "bookify/books/shared-cover"
        };

        books.Setup(x => x.FindByAsync(existing.Name))
            .ReturnsAsync(Result.Success<Book?>(existing));
        books.Setup(x => x.FindByIdAsync(existing.Id))
            .ReturnsAsync(Result.Success<Book?>(existing));
        books.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result.Success(new List<Book> { existing, other }));
        books.Setup(x => x.SaveAsync(It.IsAny<Book>()))
            .ReturnsAsync(Result.Success(true));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .ReturnsAsync(new ImageUploadResult
            {
                Success = true,
                PublicId = "bookify/books/new-cover",
                Url = "https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg",
                ThumbnailUrl = string.Empty
            });

        var (controller, _) = CreateController(books, images);
        var model = CreateModel(existing.Name);
        model.NewBook.BookId = existing.Id.ToString();
        model.NewBook.AuthorId = existing.AuthorId;
        model.NewBook.CoverImage = CreateFile("new-cover.png");

        await controller.Save(model);

        images.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Save_WhenSaveFails_DeletesNewlyUploadedImage()
    {
        var books = new Mock<IServiceRepository<Book>>();
        var images = new Mock<IImageService>();
        books.Setup(x => x.FindByAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Success<Book?>(null));
        books.Setup(x => x.SaveAsync(It.IsAny<Book>()))
            .ReturnsAsync(Result.Failure<bool>("SAVE_ERROR", "Database unavailable"));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .ReturnsAsync(new ImageUploadResult
            {
                Success = true,
                PublicId = "bookify/books/new-cover",
                Url = "https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg",
                ThumbnailUrl = string.Empty
            });

        var (controller, session) = CreateController(books, images);
        var model = CreateModel();
        model.NewBook.CoverImage = CreateFile("cover.png");

        await controller.Save(model);

        images.Verify(x => x.DeleteAsync("bookify/books/new-cover"), Times.Once);
        Assert.Equal(Helper.Error, session.GetString(Helper.MsgType));
    }

    private static (BooksController Controller, TestSession Session) CreateController(
        Mock<IServiceRepository<Book>> books,
        Mock<IImageService> images)
    {
        var localizer = new Mock<IStringLocalizer<ResourceWeb>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var session = new TestSession();
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<ISessionFeature>(new TestSessionFeature { Session = session });

        var controller = new BooksController(
            books.Object,
            Mock.Of<IServiceRepository<Author>>(),
            Mock.Of<IServiceRepository<Category>>(),
            Mock.Of<IServiceRepository<SubCategory>>(),
            images.Object,
            localizer.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return (controller, session);
    }

    private static BookViewModel CreateModel(string name = "Clean Code") => new()
    {
        NewBook = new NewBook
        {
            Name = name,
            AuthorId = Guid.NewGuid(),
            Publisher = "Test Press",
            PublishingDate = new DateTime(2020, 1, 1),
            Hall = "A1",
            IsAvailableForRental = true
        }
    };

    private static IFormFile CreateFile(string fileName)
    {
        var content = new byte[] { 1, 2, 3, 4 };
        return new FormFile(new MemoryStream(content), 0, content.Length, "NewBook.CoverImage", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = default!;
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _values.Keys;

        public void Clear() => _values.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _values.Remove(key);

        public void Set(string key, byte[] value) => _values[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value!);

        public string? GetString(string key) =>
            _values.TryGetValue(key, out var value) ? Encoding.UTF8.GetString(value) : null;
    }
}
