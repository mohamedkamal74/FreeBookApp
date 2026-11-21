using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreeBook.Tests;

public class RepositoryTests
{
    private static FreeBookDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseInMemoryDatabase($"FreeBookTests_{Guid.NewGuid():N}")
            .Options);

    private static ILogger<T> CreateLogger<T>() => Mock.Of<ILogger<T>>();

    private static async Task<Author> SeedAuthorAsync(FreeBookDbContext context, Guid id)
    {
        var author = new Author { Id = id, Name = "Test Author" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        return author;
    }

    [Fact]
    public async Task Save_NewCategory_PersistsAndCanBeFound()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        var category = new Category { Name = "Fiction", Description = "Novels" };

        var saveResult = await service.SaveAsync(category);
        Assert.True(saveResult.IsSuccess);

        var findResult = await service.FindByIdAsync(category.Id);
        Assert.True(findResult.IsSuccess);
        var saved = findResult.Value;
        Assert.NotNull(saved);
        Assert.Equal("Fiction", saved!.Name);
        Assert.Equal((int)Helper.ECurrentState.Active, saved.Currentstste);
    }

    [Fact]
    public async Task Save_ExistingCategory_UpdatesFields()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        var category = new Category { Name = "Fiction" };
        var saveResult1 = await service.SaveAsync(category);
        Assert.True(saveResult1.IsSuccess);

        category.Name = "Science";
        var saveResult2 = await service.SaveAsync(category);
        Assert.True(saveResult2.IsSuccess);

        var findResult = await service.FindByIdAsync(category.Id);
        Assert.True(findResult.IsSuccess);
        Assert.Equal("Science", findResult.Value!.Name);
    }

    [Fact]
    public async Task Delete_MarksCategoryAsDeleted()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        var category = new Category { Name = "Fiction" };
        var saveResult = await service.SaveAsync(category);
        Assert.True(saveResult.IsSuccess);

        var deleteResult = await service.DeleteAsync(category.Id);
        Assert.True(deleteResult.IsSuccess);

        var findResult = await service.FindByIdAsync(category.Id);
        Assert.True(findResult.IsSuccess);
        Assert.Null(findResult.Value);

        var allResult = await service.GetAllAsync();
        Assert.True(allResult.IsSuccess);
        Assert.Empty(allResult.Value);
    }

    [Fact]
    public async Task FindBy_TrimsAndMatchesName()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        var saveResult = await service.SaveAsync(new Category { Name = "Fiction" });
        Assert.True(saveResult.IsSuccess);

        var findResult = await service.FindByAsync("  Fiction  ");
        Assert.True(findResult.IsSuccess);
        Assert.NotNull(findResult.Value);
    }

    [Fact]
    public async Task LogService_Save_CreatesLogEntry()
    {
        using var context = CreateContext();
        var categoryService = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        var logService = new ServiceLogCategory(context, CreateLogger<ServiceLogCategory>());
        var category = new Category { Name = "Fiction" };
        var saveResult = await categoryService.SaveAsync(category);
        Assert.True(saveResult.IsSuccess);

        var userId = Guid.NewGuid();
        var logSaveResult = await logService.SaveAsync(category.Id, userId);
        Assert.True(logSaveResult.IsSuccess);

        var logsResult = await logService.GetAllAsync();
        Assert.True(logsResult.IsSuccess);
        var logs = logsResult.Value ?? new List<LogCategory>();
        Assert.Single(logs);
        Assert.Equal(Helper.Save, logs[0].Action);
        Assert.Equal(userId, logs[0].UserId);
        Assert.Equal(category.Id, logs[0].CategoryId);
    }

    [Fact]
    public async Task LogService_Delete_CreatesLogEntryWithDeleteAction()
    {
        using var context = CreateContext();
        var categoryService = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        var logService = new ServiceLogCategory(context, CreateLogger<ServiceLogCategory>());
        var category = new Category { Name = "Fiction" };
        var saveResult = await categoryService.SaveAsync(category);
        Assert.True(saveResult.IsSuccess);

        var logDeleteResult = await logService.DeleteAsync(category.Id, Guid.NewGuid());
        Assert.True(logDeleteResult.IsSuccess);

        var logsResult = await logService.GetAllAsync();
        Assert.True(logsResult.IsSuccess);
        var logs = logsResult.Value ?? new List<LogCategory>();
        Assert.Single(logs);
        Assert.Equal(Helper.Delete, logs[0].Action);
    }

    [Fact]
    public async Task Save_NewBook_PersistsAndCanBeFound()
    {
        using var context = CreateContext();
        var service = new ServicesBook(context, CreateLogger<ServicesBook>());
        var authorId = Guid.NewGuid();
        await SeedAuthorAsync(context, authorId);
        var categoryId = Guid.NewGuid();
        var book = new Book
        {
            Name = "Clean Code",
            AuthorId = authorId,
            Publisher = "Prentice Hall",
            PublishingDate = new DateTime(2008, 8, 1),
            Hall = "A1",
            Description = "A handbook of agile software craftsmanship.",
            CategoryId = categoryId
        };

        var saveResult = await service.SaveAsync(book);
        Assert.True(saveResult.IsSuccess);

        var findResult = await service.FindByIdAsync(book.Id);
        Assert.True(findResult.IsSuccess);
        var saved = findResult.Value;
        Assert.NotNull(saved);
        Assert.Equal("Clean Code", saved!.Name);
        Assert.Equal(authorId, saved.AuthorId);
        Assert.Equal(categoryId, saved.CategoryId);
        Assert.True(saved.IsAvailableForRental);
        Assert.Equal((int)Helper.ECurrentState.Active, saved.CurrentState);
    }

    [Fact]
    public async Task Save_ExistingBook_UpdatesFields()
    {
        using var context = CreateContext();
        var service = new ServicesBook(context, CreateLogger<ServicesBook>());
        var authorId = Guid.NewGuid();
        await SeedAuthorAsync(context, authorId);
        var book = new Book { Name = "Clean Code", AuthorId = authorId };
        Assert.True((await service.SaveAsync(book)).IsSuccess);

        book.Name = "Clean Architecture";
        book.Publisher = "Prentice Hall";
        book.Hall = "B2";
        book.IsAvailableForRental = false;
        Assert.True((await service.SaveAsync(book)).IsSuccess);

        var findResult = await service.FindByIdAsync(book.Id);
        var saved = findResult.Value;
        Assert.NotNull(saved);
        Assert.Equal("Clean Architecture", saved!.Name);
        Assert.Equal("Prentice Hall", saved.Publisher);
        Assert.Equal("B2", saved.Hall);
        Assert.False(saved.IsAvailableForRental);
    }

    [Fact]
    public async Task FindBy_TrimsAndMatchesBookName()
    {
        using var context = CreateContext();
        var service = new ServicesBook(context, CreateLogger<ServicesBook>());
        var authorId = Guid.NewGuid();
        await SeedAuthorAsync(context, authorId);
        Assert.True((await service.SaveAsync(new Book { Name = "The Pragmatic Programmer", AuthorId = authorId })).IsSuccess);

        var findResult = await service.FindByAsync("  The Pragmatic Programmer  ");
        Assert.True(findResult.IsSuccess);
        Assert.NotNull(findResult.Value);
    }

    [Fact]
    public async Task Delete_MarksBookAsDeleted()
    {
        using var context = CreateContext();
        var service = new ServicesBook(context, CreateLogger<ServicesBook>());
        var authorId = Guid.NewGuid();
        await SeedAuthorAsync(context, authorId);
        var book = new Book { Name = "Refactoring", AuthorId = authorId };
        Assert.True((await service.SaveAsync(book)).IsSuccess);

        var deleteResult = await service.DeleteAsync(book.Id);
        Assert.True(deleteResult.IsSuccess);

        var findResult = await service.FindByIdAsync(book.Id);
        Assert.True(findResult.IsSuccess);
        Assert.Null(findResult.Value);

        var allResult = await service.GetAllAsync();
        Assert.True(allResult.IsSuccess);
        Assert.DoesNotContain(allResult.Value!, x => x.Id == book.Id);
    }

    [Fact]
    public async Task Save_NewSubCategory_PersistsAndAppearsInGetAll()
    {
        using var context = CreateContext();
        var service = new ServicesSubCategory(context, CreateLogger<ServicesSubCategory>());
        var categoryId = Guid.NewGuid();
        var subCategory = new SubCategory { Name = "Software Engineering", CategoryId = categoryId };

        var saveResult = await service.SaveAsync(subCategory);
        Assert.True(saveResult.IsSuccess);

        var allResult = await service.GetAllAsync();
        Assert.True(allResult.IsSuccess);
        var saved = Assert.Single(allResult.Value!);
        Assert.Equal("Software Engineering", saved.Name);
        Assert.Equal(categoryId, saved.CategoryId);
        Assert.Equal((int)Helper.ECurrentState.Active, saved.Currentstate);
    }
}
