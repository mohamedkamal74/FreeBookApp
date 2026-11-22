using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Infrastructure.ViewModel;
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

        var saveResult = await service.SaveAsync(new CategoryCommand { Name = "Fiction", Description = "Novels" });
        Assert.True(saveResult.IsSuccess);

        var findResult = await service.FindByAsync("Fiction");
        Assert.True(findResult.IsSuccess);
        var saved = findResult.Value;
        Assert.NotNull(saved);
        Assert.Equal("Fiction", saved!.Name);
        Assert.Equal("Novels", saved.Description);
        Assert.Equal((int)Helper.ECurrentState.Active, saved.Currentstste);
    }

    [Fact]
    public async Task Save_ExistingCategory_UpdatesFields()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        Assert.True((await service.SaveAsync(new CategoryCommand { Name = "Fiction" })).IsSuccess);
        var id = (await service.FindByAsync("Fiction")).Value!.Id;

        var saveResult = await service.SaveAsync(
            new CategoryCommand { Id = id, Name = "Science", Description = "Physics" }, id);
        Assert.True(saveResult.IsSuccess);

        var findResult = await service.FindByIdAsync(id);
        Assert.True(findResult.IsSuccess);
        Assert.Equal("Science", findResult.Value!.Name);
        Assert.Equal("Physics", findResult.Value.Description);
    }

    [Fact]
    public async Task Delete_MarksCategoryAsDeleted()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        await service.SaveAsync(new CategoryCommand { Name = "Fiction" });
        var id = (await service.FindByAsync("Fiction")).Value!.Id;

        var deleteResult = await service.DeleteAsync(id);
        Assert.True(deleteResult.IsSuccess);

        Assert.Null((await service.FindByIdAsync(id)).Value);
        Assert.Empty((await service.GetAllAsync()).Value!);
    }

    [Fact]
    public async Task FindBy_TrimsAndMatchesName()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        Assert.True((await service.SaveAsync(new CategoryCommand { Name = "Fiction" })).IsSuccess);

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
        await categoryService.SaveAsync(new CategoryCommand { Name = "Fiction" });
        var id = (await categoryService.FindByAsync("Fiction")).Value!.Id;

        var userId = Guid.NewGuid();
        var logSaveResult = await logService.SaveAsync(id, userId);
        Assert.True(logSaveResult.IsSuccess);

        var logsResult = await logService.GetAllAsync();
        Assert.True(logsResult.IsSuccess);
        var logs = logsResult.Value ?? new List<LogCategoryDto>();
        var log = Assert.Single(logs);
        Assert.Equal(Helper.Save, log.Action);
        Assert.Equal(userId, log.UserId);
        Assert.Equal(id, log.CategoryId);
        Assert.Equal("Fiction", log.category?.Name);
    }

    [Fact]
    public async Task LogService_Delete_CreatesLogEntryWithDeleteAction()
    {
        using var context = CreateContext();
        var categoryService = new ServicesCategory(context, CreateLogger<ServicesCategory>());
        var logService = new ServiceLogCategory(context, CreateLogger<ServiceLogCategory>());
        await categoryService.SaveAsync(new CategoryCommand { Name = "Fiction" });
        var id = (await categoryService.FindByAsync("Fiction")).Value!.Id;

        Assert.True((await logService.DeleteAsync(id, Guid.NewGuid())).IsSuccess);

        var logs = (await logService.GetAllAsync()).Value ?? new List<LogCategoryDto>();
        Assert.Equal(Helper.Delete, Assert.Single(logs).Action);
    }

    [Fact]
    public async Task Save_NewBook_PersistsAndCanBeFound()
    {
        using var context = CreateContext();
        var service = new ServicesBook(context, CreateLogger<ServicesBook>());
        var authorId = Guid.NewGuid();
        await SeedAuthorAsync(context, authorId);
        var categoryId = Guid.NewGuid();

        var saveResult = await service.SaveAsync(new BookCommand
        {
            Name = "Clean Code",
            AuthorId = authorId,
            Publisher = "Prentice Hall",
            PublishingDate = new DateTime(2008, 8, 1),
            Hall = "A1",
            Description = "A handbook of agile software craftsmanship.",
            CategoryId = categoryId
        });
        Assert.True(saveResult.IsSuccess);

        var saved = (await service.FindByAsync("Clean Code")).Value;
        Assert.NotNull(saved);
        Assert.Equal(authorId, saved!.AuthorId);
        Assert.Equal("Test Author", saved.Author?.Name);
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
        Assert.True((await service.SaveAsync(new BookCommand { Name = "Clean Code", AuthorId = authorId })).IsSuccess);
        var id = (await service.FindByAsync("Clean Code")).Value!.Id;

        Assert.True((await service.SaveAsync(new BookCommand
        {
            Id = id,
            Name = "Clean Architecture",
            AuthorId = authorId,
            Publisher = "Prentice Hall",
            Hall = "B2",
            IsAvailableForRental = false
        }, id)).IsSuccess);

        var saved = (await service.FindByIdAsync(id)).Value;
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
        Assert.True((await service.SaveAsync(new BookCommand
        {
            Name = "The Pragmatic Programmer",
            AuthorId = authorId
        })).IsSuccess);

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
        await service.SaveAsync(new BookCommand { Name = "Refactoring", AuthorId = authorId });
        var id = (await service.FindByAsync("Refactoring")).Value!.Id;

        Assert.True((await service.DeleteAsync(id)).IsSuccess);

        Assert.Null((await service.FindByIdAsync(id)).Value);
        Assert.DoesNotContain((await service.GetAllAsync()).Value!, x => x.Id == id);
    }

    [Fact]
    public async Task Save_NewSubCategory_PersistsAndAppearsInGetAll()
    {
        using var context = CreateContext();
        var service = new ServicesSubCategory(context, CreateLogger<ServicesSubCategory>());
        var categoryId = Guid.NewGuid();

        Assert.True((await service.SaveAsync(new SubCategoryCommand
        {
            Name = "Software Engineering",
            CategoryId = categoryId
        })).IsSuccess);

        var saved = Assert.Single((await service.GetAllAsync()).Value!);
        Assert.Equal("Software Engineering", saved.Name);
        Assert.Equal(categoryId, saved.CategoryId);
        Assert.Equal((int)Helper.ECurrentState.Active, saved.Currentstate);
    }
}
