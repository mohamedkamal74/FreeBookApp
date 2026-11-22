using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreeBook.Tests;

public class NotificationRepositoryTests
{
    private static FreeBookDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseInMemoryDatabase($"FreeBookNotifications_{Guid.NewGuid():N}")
            .Options);

    private static ServicesNotification CreateService(FreeBookDbContext context) =>
        new(context, Mock.Of<ILogger<ServicesNotification>>());

    private static NotificationCommand NewNotification(string key, string name = "Ahmed", int? days = 3) => new()
    {
        Type = NotificationKindDto.SubscriptionExpiring,
        DedupeKey = key,
        SubscriberName = name,
        Days = days,
        TargetUrl = "/Admin/Subscriptions/Subscriptions"
    };

    [Fact]
    public async Task Upsert_InsertsNewNotification()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.UpsertAsync(NewNotification("sub:1:3"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal(1, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task Upsert_SameKey_UpdatesInsteadOfDuplicating()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        await service.UpsertAsync(NewNotification("sub:1:7", "Ahmed", 7));
        var second = await service.UpsertAsync(NewNotification("sub:1:7", "Ahmed", 3));

        Assert.True(second.IsSuccess);
        Assert.False(second.Value);
        Assert.Equal(1, await context.Notifications.CountAsync());
        var stored = await context.Notifications.SingleAsync();
        Assert.Equal(3, stored.Days);
    }

    [Fact]
    public async Task GetRecent_ReturnsUnreadFirstThenNewest()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        context.Notifications.Add(new Notification
        {
            Type = NotificationType.RentalOverdue,
            DedupeKey = "overdue:1:1",
            SubscriberName = "Ahmed",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });
        context.Notifications.Add(new Notification
        {
            Type = NotificationType.RentalOverdue,
            DedupeKey = "overdue:1:2",
            SubscriberName = "Ahmed",
            CreatedOn = DateTime.UtcNow.AddDays(-1),
            IsRead = true
        });
        await context.SaveChangesAsync();

        var result = await service.GetRecentAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("overdue:1:1", result.Value[0].DedupeKey);
        Assert.Equal("overdue:1:2", result.Value[1].DedupeKey);
    }

    [Fact]
    public async Task GetRecent_RespectsTake()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        for (var i = 0; i < 5; i++)
            await service.UpsertAsync(NewNotification($"fine:{i}"));

        var result = await service.GetRecentAsync(2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task UnreadCount_And_MarkRead_Work()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.UpsertAsync(NewNotification("a"));
        await service.UpsertAsync(NewNotification("b"));

        var count = await service.GetUnreadCountAsync();
        Assert.Equal(2, count.Value);

        var stored = await context.Notifications.FirstAsync();
        var marked = await service.MarkReadAsync(stored.Id);
        Assert.True(marked.IsSuccess);
        Assert.True(marked.Value);
        Assert.Equal(1, (await service.GetUnreadCountAsync()).Value);
        Assert.NotNull((await context.Notifications.FindAsync(stored.Id))!.ReadOn);
    }

    [Fact]
    public async Task MarkAllRead_Then_DeleteRead_ClearsOnlyReadRows()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.UpsertAsync(NewNotification("a", days: 7));
        await service.UpsertAsync(NewNotification("b", days: 3));

        var marked = await service.MarkAllReadAsync();
        Assert.Equal(2, marked.Value);
        Assert.Equal(0, (await service.GetUnreadCountAsync()).Value);

        var deleted = await service.DeleteReadAsync();
        Assert.Equal(2, deleted.Value);
        Assert.Equal(0, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task DeleteRead_LeavesUnreadRows()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.UpsertAsync(NewNotification("a"));
        var stored = await context.Notifications.SingleAsync();
        await service.MarkReadAsync(stored.Id);
        await service.UpsertAsync(NewNotification("b"));

        var deleted = await service.DeleteReadAsync();

        Assert.Equal(1, deleted.Value);
        Assert.Equal(1, await context.Notifications.CountAsync());
        Assert.Equal("b", (await context.Notifications.SingleAsync()).DedupeKey);
    }

    [Fact]
    public async Task MarkRead_MissingId_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.MarkReadAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error?.Code);
    }
}
