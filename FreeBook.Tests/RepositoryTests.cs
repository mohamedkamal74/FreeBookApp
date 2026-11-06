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
    public void Save_NewCategory_PersistsAndCanBeFound()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context);
        var category = new Category { Name = "Fiction", Description = "Novels" };

        Assert.True(service.Save(category));

        var saved = service.FindById(category.Id);
        Assert.NotNull(saved);
        Assert.Equal("Fiction", saved!.Name);
        Assert.Equal((int)Helper.ECurrentState.Active, saved.Currentstste);
    }

    [Fact]
    public void Save_ExistingCategory_UpdatesFields()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context);
        var category = new Category { Name = "Fiction" };
        Assert.True(service.Save(category));

        category.Name = "Science";
        Assert.True(service.Save(category));

        var updated = service.FindById(category.Id);
        Assert.Equal("Science", updated!.Name);
    }

    [Fact]
    public void Delete_MarksCategoryAsDeleted()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context);
        var category = new Category { Name = "Fiction" };
        Assert.True(service.Save(category));

        Assert.True(service.Delete(category.Id));

        Assert.Null(service.FindById(category.Id));
        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void FindBy_TrimsAndMatchesName()
    {
        using var context = CreateContext();
        var service = new ServicesCategory(context);
        Assert.True(service.Save(new Category { Name = "Fiction" }));

        Assert.NotNull(service.FindBy("  Fiction  "));
    }

    [Fact]
    public void LogService_Save_CreatesLogEntry()
    {
        using var context = CreateContext();
        var categoryService = new ServicesCategory(context);
        var logService = new ServiceLogCategory(context);
        var category = new Category { Name = "Fiction" };
        Assert.True(categoryService.Save(category));

        var userId = Guid.NewGuid();
        Assert.True(logService.Save(category.Id, userId));

        var logs = logService.GetAll();
        Assert.Single(logs);
        Assert.Equal(Helper.Save, logs[0].Action);
        Assert.Equal(userId, logs[0].UserId);
        Assert.Equal(category.Id, logs[0].CategoryId);
    }
}
