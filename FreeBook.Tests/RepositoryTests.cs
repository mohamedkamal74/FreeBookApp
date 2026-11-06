using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Microsoft.EntityFrameworkCore;

namespace FreeBook.Tests;

public class RepositoryTests
{
    private static FreeBookDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseInMemoryDatabase($"FreeBookTests_{Guid.NewGuid():N}")
            .Options);

    [Fact]
    public async Task Save_NewCategory_PersistsAndCanBeFound()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context);
        var category = new Category { Name = "Fiction", Description = "Novels" };

        Assert.True(await service.SaveAsync(category));

        var saved = await service.FindByIdAsync(category.Id);
        Assert.NotNull(saved);
        Assert.Equal("Fiction", saved!.Name);
        Assert.Equal((int)Helper.ECurrentState.Active, saved.Currentstste);
    }

    [Fact]
    public async Task Save_ExistingCategory_UpdatesFields()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context);
        var category = new Category { Name = "Fiction" };
        Assert.True(await service.SaveAsync(category));

        category.Name = "Science";
        Assert.True(await service.SaveAsync(category));

        var updated = await service.FindByIdAsync(category.Id);
        Assert.Equal("Science", updated!.Name);
    }

    [Fact]
    public async Task Delete_MarksCategoryAsDeleted()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context);
        var category = new Category { Name = "Fiction" };
        Assert.True(await service.SaveAsync(category));

        Assert.True(await service.DeleteAsync(category.Id));

        Assert.Null(await service.FindByIdAsync(category.Id));
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task FindBy_TrimsAndMatchesName()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context);
        Assert.True(await service.SaveAsync(new Category { Name = "Fiction" }));

        Assert.NotNull(await service.FindByAsync("  Fiction  "));
    }

    [Fact]
    public async Task LogService_Save_CreatesLogEntry()
    {
        using var context = CreateContext();
        var categoryService = new ServicesCategory(context);
        var logService = new ServiceLogCategory(context);
        var category = new Category { Name = "Fiction" };
        Assert.True(await categoryService.SaveAsync(category));

        var userId = Guid.NewGuid();
        Assert.True(await logService.SaveAsync(category.Id, userId));

        var logs = await logService.GetAllAsync();
        Assert.Single(logs);
        Assert.Equal(Helper.Save, logs[0].Action);
        Assert.Equal(userId, logs[0].UserId);
        Assert.Equal(category.Id, logs[0].CategoryId);
    }

    [Fact]
    public async Task LogService_Delete_CreatesLogEntryWithDeleteAction()
    {
        using var context = CreateContext();
        var categoryService = new ServicesCategory(context);
        var logService = new ServiceLogCategory(context);
        var category = new Category { Name = "Fiction" };
        Assert.True(await categoryService.SaveAsync(category));

        Assert.True(await logService.DeleteAsync(category.Id, Guid.NewGuid()));

        var logs = await logService.GetAllAsync();
        Assert.Single(logs);
        Assert.Equal(Helper.Delete, logs[0].Action);
    }
}
