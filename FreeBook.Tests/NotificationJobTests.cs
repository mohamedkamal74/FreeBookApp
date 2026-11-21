using Domin.Entity;
using FreeBook.Jobs;
using Infrastructure.Common;
using Infrastructure.Common.Configuration;
using Infrastructure.Common.Interfaces;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.IRepository.ServiceRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FreeBook.Tests;

public class NotificationJobTests
{
    private static FreeBookDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseInMemoryDatabase($"FreeBookJobNotifications_{Guid.NewGuid():N}")
            .Options);

    private static Subscriber NewSubscriber() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Ahmed",
        LastName = "Ali",
        NationalId = "12345678901234",
        MobileNumber = "0501234567",
        Email = "ahmed@example.com",
        AreaId = Guid.NewGuid(),
        GovernorateId = Guid.NewGuid()
    };

    [Fact]
    public async Task ExpiryCheckJob_WritesOneNotificationPerStage()
    {
        using var context = CreateContext();
        var notificationRepo = new ServicesNotification(context, Mock.Of<ILogger<ServicesNotification>>());
        var subscriber = NewSubscriber();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriber.Id,
            EndDate = DateTime.UtcNow.AddDays(2)
        };

        var subRepo = new Mock<ISubscriptionRepository>();
        subRepo.Setup(x => x.GetExpiringSubscriptionsAsync(7))
            .ReturnsAsync(Result.Success(new List<Subscription> { subscription }));
        var subscriberRepo = new Mock<IServiceRepository<Subscriber>>();
        subscriberRepo.Setup(x => x.FindByIdAsync(subscriber.Id))
            .ReturnsAsync(Result.Success<Subscriber?>(subscriber));

        var job = new ExpiryCheckJob(subRepo.Object, subscriberRepo.Object, notificationRepo,
            Mock.Of<IEmailService>(), Mock.Of<ISmsService>(), Mock.Of<ILogger<ExpiryCheckJob>>());

        await job.CheckExpiringSubscriptionsAsync();
        await job.CheckExpiringSubscriptionsAsync();

        var stored = await context.Notifications.SingleAsync();
        Assert.Equal(NotificationType.SubscriptionExpiring, stored.Type);
        Assert.Equal($"sub:{subscription.Id}:3", stored.DedupeKey);
        Assert.Equal(3, stored.Days);
        Assert.Equal("Ahmed Ali", stored.SubscriberName);
    }

    [Fact]
    public async Task ExpiryCheckJob_UsesOneDayStageForTomorrow()
    {
        using var context = CreateContext();
        var notificationRepo = new ServicesNotification(context, Mock.Of<ILogger<ServicesNotification>>());
        var subscriber = NewSubscriber();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriber.Id,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var subRepo = new Mock<ISubscriptionRepository>();
        subRepo.Setup(x => x.GetExpiringSubscriptionsAsync(7))
            .ReturnsAsync(Result.Success(new List<Subscription> { subscription }));
        var subscriberRepo = new Mock<IServiceRepository<Subscriber>>();
        subscriberRepo.Setup(x => x.FindByIdAsync(subscriber.Id))
            .ReturnsAsync(Result.Success<Subscriber?>(subscriber));

        var job = new ExpiryCheckJob(subRepo.Object, subscriberRepo.Object, notificationRepo,
            Mock.Of<IEmailService>(), Mock.Of<ISmsService>(), Mock.Of<ILogger<ExpiryCheckJob>>());

        await job.CheckExpiringSubscriptionsAsync();

        var stored = await context.Notifications.SingleAsync();
        Assert.Equal($"sub:{subscription.Id}:1", stored.DedupeKey);
        Assert.Equal(1, stored.Days);
    }

    private static Rental NewOverdueRental(Subscriber subscriber, out Guid copyId)
    {
        copyId = Guid.NewGuid();
        var book = new Book { Id = Guid.NewGuid(), Name = "Clean Code" };
        var copy = new BookCopy
        {
            Id = copyId,
            BookId = book.Id,
            Book = book,
            EditionNumber = 1,
            SerialNumber = 1
        };
        return new Rental
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriber.Id,
            StartDate = DateTime.UtcNow.AddDays(-20),
            RentalCopies = new List<RentalCopy>
            {
                new()
                {
                    RentalId = Guid.Empty,
                    BookCopyId = copyId,
                    BookCopy = copy,
                    EndDate = DateTime.UtcNow.AddDays(-4)
                }
            }
        };
    }

    [Fact]
    public async Task OverdueJob_WritesOneNotificationPerOverdueCopy()
    {
        using var context = CreateContext();
        var notificationRepo = new ServicesNotification(context, Mock.Of<ILogger<ServicesNotification>>());
        var subscriber = NewSubscriber();
        var rental = NewOverdueRental(subscriber, out var copyId);

        var rentalRepo = new Mock<IRentalRepository>();
        rentalRepo.Setup(x => x.GetOverdueRentalsAsync())
            .ReturnsAsync(Result.Success(new List<Rental> { rental }));
        var subscriberRepo = new Mock<IServiceRepository<Subscriber>>();
        subscriberRepo.Setup(x => x.FindByIdAsync(subscriber.Id))
            .ReturnsAsync(Result.Success<Subscriber?>(subscriber));

        var job = new OverdueNotificationJob(rentalRepo.Object, subscriberRepo.Object, notificationRepo,
            Mock.Of<IEmailService>(), Mock.Of<ISmsService>(), Mock.Of<ILogger<OverdueNotificationJob>>());

        await job.SendOverdueNotificationsAsync();
        await job.SendOverdueNotificationsAsync();

        var stored = await context.Notifications.SingleAsync();
        Assert.Equal(NotificationType.RentalOverdue, stored.Type);
        Assert.Equal($"overdue:{rental.Id}:{copyId}", stored.DedupeKey);
        Assert.Equal("Clean Code", stored.BookTitle);
        Assert.Equal(4, stored.Days);
    }

    [Fact]
    public async Task FineJob_WritesNotificationWithCalculatedAmount()
    {
        using var context = CreateContext();
        var notificationRepo = new ServicesNotification(context, Mock.Of<ILogger<ServicesNotification>>());
        var subscriber = NewSubscriber();
        var rental = NewOverdueRental(subscriber, out var copyId);
        rental.TotalFine = 0;

        var rentalRepo = new Mock<IRentalRepository>();
        rentalRepo.Setup(x => x.GetOverdueRentalsAsync())
            .ReturnsAsync(Result.Success(new List<Rental> { rental }));
        rentalRepo.Setup(x => x.SaveAsync(rental)).ReturnsAsync(Result.Success(true));
        var subscriberRepo = new Mock<IServiceRepository<Subscriber>>();
        subscriberRepo.Setup(x => x.FindByIdAsync(subscriber.Id))
            .ReturnsAsync(Result.Success<Subscriber?>(subscriber));

        var job = new FineCalculationJob(rentalRepo.Object, subscriberRepo.Object, notificationRepo,
            Mock.Of<IEmailService>(), Mock.Of<ISmsService>(),
            Options.Create(new FineSettings()), Mock.Of<ILogger<FineCalculationJob>>());

        await job.CalculateDailyFinesAsync();
        await job.CalculateDailyFinesAsync();

        var stored = await context.Notifications.SingleAsync();
        Assert.Equal(NotificationType.FineApplied, stored.Type);
        Assert.Equal($"fine:{rental.Id}:{copyId}", stored.DedupeKey);
        Assert.Equal(4m, stored.Amount);
        Assert.Equal("Clean Code", stored.BookTitle);
        Assert.Equal(8m, rental.TotalFine);
    }
}
