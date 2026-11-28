using FreeBook.Areas.Admin.Controllers;
using FreeBook.Resources;
using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;

namespace FreeBook.Tests;

public class BookCoverUploadTests
{
    [Fact]
    public async Task Save_WithValidCover_UploadsImageAndStoresCloudinaryFields()
    {
        var books = new Mock<IBookRepository>();
        var images = new Mock<IImageService>();
        var flash = new Mock<IFlashMessageService>();
        BookCommand? saved = null;

        books.Setup(x => x.FindByAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Success<BookDto?>(null));
        books.Setup(x => x.SaveAsync(It.IsAny<BookCommand>(), It.IsAny<Guid?>()))
            .Callback<BookCommand, Guid?>((book, _) => saved = book)
            .ReturnsAsync(Result.Success(true));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .ReturnsAsync(new ImageUploadResult
            {
                Success = true,
                PublicId = "bookify/books/new-cover",
                Url = "https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg",
                ThumbnailUrl = "https://res.cloudinary.com/demo/image/upload/c_fill,w_200,h_200/bookify/books/new-cover.jpg"
            });

        var controller = CreateController(books, images, flash);
        var model = CreateModel();
        model.NewBook.CoverImage = CreateFile("cover.png");

        var result = await controller.Save(model);

        Assert.IsType<RedirectToActionResult>(result);
        images.Verify(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"), Times.Once);
        Assert.NotNull(saved);
        Assert.Equal("bookify/books/new-cover", saved!.ImagePublicId);
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg", saved.ImageUrl);
        Assert.Contains("c_fill,w_200,h_200", saved.ImageThumbnailUrl);
        flash.Verify(x => x.Success(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        flash.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Save_WithInvalidCoverExtension_DoesNotUploadOrSave()
    {
        var books = new Mock<IBookRepository>();
        var images = new Mock<IImageService>();
        var flash = new Mock<IFlashMessageService>();
        var controller = CreateController(books, images, flash);
        var model = CreateModel();
        model.NewBook.CoverImage = CreateFile("cover.txt");

        var result = await controller.Save(model);

        Assert.IsType<RedirectToActionResult>(result);
        flash.Verify(x => x.Error(It.IsAny<string>(), "msgCoverImageInvalid"), Times.Once);
        images.Verify(x => x.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        images.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        books.Verify(x => x.SaveAsync(It.IsAny<BookCommand>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Save_WhenCloudinaryUploadFails_DoesNotSaveBook()
    {
        var books = new Mock<IBookRepository>();
        var images = new Mock<IImageService>();
        var flash = new Mock<IFlashMessageService>();
        books.Setup(x => x.FindByAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Success<BookDto?>(null));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .ReturnsAsync(new ImageUploadResult { Success = false, Error = "Cloudinary unavailable" });

        var controller = CreateController(books, images, flash);
        var model = CreateModel();
        model.NewBook.CoverImage = CreateFile("cover.png");

        var result = await controller.Save(model);

        Assert.IsType<RedirectToActionResult>(result);
        flash.Verify(x => x.Error(It.IsAny<string>(), "msgCoverImageUploadFailed"), Times.Once);
        books.Verify(x => x.SaveAsync(It.IsAny<BookCommand>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Save_ReplaceCover_DeletesOldImageOnlyAfterSuccessfulSave()
    {
        var books = new Mock<IBookRepository>();
        var images = new Mock<IImageService>();
        var events = new List<string>();
        var existing = new BookDto
        {
            Id = Guid.NewGuid(),
            Name = "Clean Code",
            AuthorId = Guid.NewGuid(),
            ImagePublicId = "bookify/books/old-cover"
        };

        books.Setup(x => x.FindByAsync(existing.Name))
            .ReturnsAsync(Result.Success<BookDto?>(existing));
        books.Setup(x => x.FindByIdAsync(existing.Id))
            .ReturnsAsync(Result.Success<BookDto?>(existing));
        books.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result.Success(new List<BookDto> { existing }));
        books.Setup(x => x.SaveAsync(It.IsAny<BookCommand>(), It.IsAny<Guid?>()))
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

        var controller = CreateController(books, images, new Mock<IFlashMessageService>());
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
        var books = new Mock<IBookRepository>();
        var images = new Mock<IImageService>();
        var existing = new BookDto
        {
            Id = Guid.NewGuid(),
            Name = "Clean Code",
            AuthorId = Guid.NewGuid(),
            ImagePublicId = "bookify/books/shared-cover"
        };
        var other = new BookDto
        {
            Id = Guid.NewGuid(),
            Name = "Clean Architecture",
            AuthorId = Guid.NewGuid(),
            ImagePublicId = "bookify/books/shared-cover"
        };

        books.Setup(x => x.FindByAsync(existing.Name))
            .ReturnsAsync(Result.Success<BookDto?>(existing));
        books.Setup(x => x.FindByIdAsync(existing.Id))
            .ReturnsAsync(Result.Success<BookDto?>(existing));
        books.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result.Success(new List<BookDto> { existing, other }));
        books.Setup(x => x.SaveAsync(It.IsAny<BookCommand>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Success(true));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .ReturnsAsync(new ImageUploadResult
            {
                Success = true,
                PublicId = "bookify/books/new-cover",
                Url = "https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg",
                ThumbnailUrl = string.Empty
            });

        var controller = CreateController(books, images, new Mock<IFlashMessageService>());
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
        var books = new Mock<IBookRepository>();
        var images = new Mock<IImageService>();
        var flash = new Mock<IFlashMessageService>();
        books.Setup(x => x.FindByAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Success<BookDto?>(null));
        books.Setup(x => x.SaveAsync(It.IsAny<BookCommand>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Failure<bool>("SAVE_ERROR", "Database unavailable"));
        images.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "books"))
            .ReturnsAsync(new ImageUploadResult
            {
                Success = true,
                PublicId = "bookify/books/new-cover",
                Url = "https://res.cloudinary.com/demo/image/upload/bookify/books/new-cover.jpg",
                ThumbnailUrl = string.Empty
            });

        var controller = CreateController(books, images, flash);
        var model = CreateModel();
        model.NewBook.CoverImage = CreateFile("cover.png");

        await controller.Save(model);

        images.Verify(x => x.DeleteAsync("bookify/books/new-cover"), Times.Once);
        flash.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    private static BooksController CreateController(
        Mock<IBookRepository> books,
        Mock<IImageService> images,
        Mock<IFlashMessageService> flash)
    {
        var localizer = new Mock<IStringLocalizer<ResourceWeb>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        return new BooksController(
            books.Object,
            Mock.Of<IServiceRepository<AuthorDto, AuthorCommand>>(),
            Mock.Of<IServiceRepository<CategoryDto, CategoryCommand>>(),
            Mock.Of<IServiceRepository<SubCategoryDto, SubCategoryCommand>>(),
            images.Object,
            localizer.Object,
            flash.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
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
}
