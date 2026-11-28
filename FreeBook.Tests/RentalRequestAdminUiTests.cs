using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class RentalRequestAdminUiTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Admin@12345";
    private const string AdminEmail = "rental-request-admin@test.local";

    private readonly CustomWebApplicationFactory _factory;

    public RentalRequestAdminUiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubscribersPage_ExposesUserLinkDropdownAndPersistsSelection()
    {
        await EnsureAdminAsync();
        var user = await SeedRoleUserAsync();
        var client = await CreateLoggedInClientAsync();

        var page = await client.GetStringAsync("/Admin/Subscribers/Subscribers?culture=en");
        Assert.Contains("id=\"applicationUserId\"", page);
        Assert.Contains("Linked User", page);
        Assert.Contains(user.Name, page);

        var nationalId = $"77{Guid.NewGuid().ToString("N")[..9]}";
        var saveResponse = await client.PostAsync("/Admin/Subscribers/Save?culture=en", BuildForm(new[]
        {
            ("NewSubscriber.SubscriberId", string.Empty),
            ("NewSubscriber.FirstName", "Linked"),
            ("NewSubscriber.LastName", "Member"),
            ("NewSubscriber.NationalId", nationalId),
            ("NewSubscriber.MobileNumber", "01012345678"),
            ("NewSubscriber.Email", $"linked-{nationalId}@test.local"),
            ("NewSubscriber.DateOfBirth", "1990-01-01"),
            ("NewSubscriber.AreaId", user.AreaId.ToString()),
            ("NewSubscriber.GovernorateId", user.GovernorateId.ToString()),
            ("NewSubscriber.ApplicationUserId", user.UserId)
        }, ExtractToken(page)));
        Assert.Equal(HttpStatusCode.Redirect, saveResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();
            var subscriber = await context.Subscribers.SingleAsync(x => x.NationalId == nationalId);
            Assert.Equal(user.UserId, subscriber.ApplicationUserId);
        }

        var afterSave = await client.GetStringAsync("/Admin/Subscribers/Subscribers?culture=en");
        var invalidResponse = await client.PostAsync("/Admin/Subscribers/Save?culture=en", BuildForm(new[]
        {
            ("NewSubscriber.SubscriberId", string.Empty),
            ("NewSubscriber.FirstName", "Invalid"),
            ("NewSubscriber.LastName", "Link"),
            ("NewSubscriber.NationalId", $"88{Guid.NewGuid().ToString("N")[..9]}"),
            ("NewSubscriber.MobileNumber", "01087654321"),
            ("NewSubscriber.Email", $"invalid-{Guid.NewGuid():N}@test.local"),
            ("NewSubscriber.DateOfBirth", "1990-01-01"),
            ("NewSubscriber.AreaId", user.AreaId.ToString()),
            ("NewSubscriber.GovernorateId", user.GovernorateId.ToString()),
            ("NewSubscriber.ApplicationUserId", Guid.NewGuid().ToString())
        }, ExtractToken(afterSave)));
        Assert.Equal(HttpStatusCode.Redirect, invalidResponse.StatusCode);

        var afterInvalid = await client.GetStringAsync("/Admin/Subscribers/Subscribers?culture=en");
        Assert.Contains("Select a user with the User role", afterInvalid);
    }

    [Fact]
    public async Task RentalsPage_ShowsLocalizedPendingRequestsAndActionHooks()
    {
        await EnsureAdminAsync();
        var seed = await SeedPendingRequestAsync();
        var client = await CreateLoggedInClientAsync();

        var english = await client.GetStringAsync("/Admin/Rentals/Rentals?culture=en");
        Assert.Contains("id=\"pendingRentalRequests\"", english);
        Assert.Contains("id=\"tableRentalRequests\"", english);
        Assert.Contains($"id=\"btnApproveRequest_{seed.RequestId}\"", english);
        Assert.Contains($"id=\"btnRejectRequest_{seed.RequestId}\"", english);
        Assert.Contains("Rental Requests", english);
        Assert.Contains("Request Book", english);

        var cultureResponse = await client.PostAsync("/Localization/SetCulture", BuildForm(new[]
        {
            ("culture", "ar"),
            ("returnUrl", "/Admin/Rentals/Rentals")
        }, ExtractToken(english)));
        Assert.Equal(HttpStatusCode.Redirect, cultureResponse.StatusCode);

        var arabic = WebUtility.HtmlDecode(await client.GetStringAsync(cultureResponse.Headers.Location!));
        Assert.Contains("طلبات الاستعارة", arabic);
        Assert.Contains("اعتماد", arabic);
    }

    [Fact]
    public async Task ApproveRequest_CreatesRentalAndRejectRequest_AreLocalized()
    {
        await EnsureAdminAsync();
        var approvedSeed = await SeedPendingRequestAsync();
        var client = await CreateLoggedInClientAsync();

        var page = await client.GetStringAsync("/Admin/Rentals/Rentals?culture=en");
        var approveResponse = await client.PostAsync("/Admin/Rentals/ApproveRequest?culture=en", BuildForm(new[]
        {
            ("RequestId", approvedSeed.RequestId.ToString()),
            ("SelectedCopyIds", approvedSeed.CopyId.ToString())
        }, ExtractToken(page)));
        Assert.Equal(HttpStatusCode.Redirect, approveResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();
            Assert.True(await context.Rentals.AnyAsync(x => x.SubscriberId == approvedSeed.SubscriberId));
            Assert.False((await context.BookCopies.SingleAsync(x => x.Id == approvedSeed.CopyId)).IsAvailableForRental);
            Assert.Equal(RentalRequestStatus.Approved,
                (await context.RentalRequests.SingleAsync(x => x.Id == approvedSeed.RequestId)).Status);
        }

        var afterApprove = await client.GetStringAsync("/Admin/Rentals/Rentals?culture=en");
        Assert.Contains("Rental request approved successfully", afterApprove);
        Assert.DoesNotContain($"id=\"btnApproveRequest_{approvedSeed.RequestId}\"", afterApprove);

        var rejectedSeed = await SeedPendingRequestAsync();
        var rejectPage = await client.GetStringAsync("/Admin/Rentals/Rentals?culture=en");
        var rejectResponse = await client.PostAsync("/Admin/Rentals/RejectRequest?culture=en", BuildForm(new[]
        {
            ("RequestId", rejectedSeed.RequestId.ToString()),
            ("RejectionReason", "Not available")
        }, ExtractToken(rejectPage)));
        Assert.Equal(HttpStatusCode.Redirect, rejectResponse.StatusCode);

        var afterReject = await client.GetStringAsync("/Admin/Rentals/Rentals?culture=en");
        Assert.Contains("Rental request rejected", afterReject);
        Assert.DoesNotContain($"id=\"btnRejectRequest_{rejectedSeed.RequestId}\"", afterReject);
    }

    [Fact]
    public async Task ApproveRequest_WithUnavailableCopy_ShowsLocalizedError()
    {
        await EnsureAdminAsync();
        var seed = await SeedPendingRequestAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();
            var copy = await context.BookCopies.SingleAsync(x => x.Id == seed.CopyId);
            copy.IsAvailableForRental = false;
            await context.SaveChangesAsync();
        }

        var client = await CreateLoggedInClientAsync();
        var page = await client.GetStringAsync("/Admin/Rentals/Rentals?culture=en");
        var response = await client.PostAsync("/Admin/Rentals/ApproveRequest?culture=en", BuildForm(new[]
        {
            ("RequestId", seed.RequestId.ToString()),
            ("SelectedCopyIds", seed.CopyId.ToString())
        }, ExtractToken(page)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var after = await client.GetStringAsync("/Admin/Rentals/Rentals?culture=en");
        Assert.Contains("One or more selected copies are not available", after);
        Assert.Contains($"id=\"btnApproveRequest_{seed.RequestId}\"", after);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<HttpClient> CreateLoggedInClientAsync()
    {
        var client = CreateClient();
        var token = ExtractToken(await client.GetStringAsync("/Admin?culture=en"));
        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en", BuildForm(new[]
        {
            ("Email", AdminEmail),
            ("Password", Password),
            ("RememberMe", "false")
        }, token));
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Unexpected login status: {response.StatusCode}");
        return client;
    }

    private async Task EnsureAdminAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            Assert.True((await roleManager.CreateAsync(new IdentityRole("Admin"))).Succeeded);
        if (!await roleManager.RoleExistsAsync("User"))
            Assert.True((await roleManager.CreateAsync(new IdentityRole("User"))).Succeeded);

        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                Name = "Rental Request Admin",
                ActiveUser = true,
                EmailConfirmed = true,
                ImageUser = string.Empty
            };
            Assert.True((await userManager.CreateAsync(admin, Password)).Succeeded);
            Assert.True((await userManager.AddToRoleAsync(admin, "Admin")).Succeeded);
        }
    }

    private async Task<RoleUser> SeedRoleUserAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"request-member-{suffix}@test.local";
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("User"))
            Assert.True((await roleManager.CreateAsync(new IdentityRole("User"))).Succeeded);

        var user = new ApplicationUser
        {
            Id = $"member-{suffix}",
            UserName = email,
            Email = email,
            Name = $"Member {suffix}",
            ActiveUser = true,
            EmailConfirmed = true,
            ImageUser = string.Empty
        };
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, "User")).Succeeded);

        var governorateId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        using var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();
        context.AddRange(
            new Governorate { Id = governorateId, Name = $"UI Gov {suffix}" },
            new Area { Id = areaId, Name = $"UI Area {suffix}", GovernorateId = governorateId });
        await context.SaveChangesAsync();

        return new RoleUser(user.Id, user.Name!, areaId, governorateId);
    }

    private async Task<PendingSeed> SeedPendingRequestAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"order-{suffix}@test.local";
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var requests = scope.ServiceProvider.GetRequiredService<IRentalRequestRepository>();

        if (!await roleManager.RoleExistsAsync("User"))
            Assert.True((await roleManager.CreateAsync(new IdentityRole("User"))).Succeeded);

        var user = new ApplicationUser
        {
            Id = $"order-user-{suffix}",
            UserName = email,
            Email = email,
            Name = $"Order Member {suffix}",
            ActiveUser = true,
            EmailConfirmed = true,
            ImageUser = string.Empty
        };
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, "User")).Succeeded);

        var now = DateTime.UtcNow;
        var governorateId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var copyId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();

        using var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();
        context.AddRange(
            new Governorate { Id = governorateId, Name = $"Order Gov {suffix}" },
            new Area { Id = areaId, Name = $"Order Area {suffix}", GovernorateId = governorateId },
            new Book
            {
                Id = bookId,
                Name = "Request Book",
                AuthorId = Guid.NewGuid(),
                IsAvailableForRental = true,
                CurrentState = (int)Helper.ECurrentState.Active
            },
            new BookCopy
            {
                Id = copyId,
                BookId = bookId,
                EditionNumber = 1,
                SerialNumber = 1,
                IsAvailableForRental = true
            },
            new Subscriber
            {
                Id = subscriberId,
                FirstName = $"Order{suffix}",
                LastName = "Member",
                DateOfBirth = now.AddYears(-30),
                NationalId = $"66{suffix}",
                MobileNumber = $"015{suffix}",
                Email = $"order-subscriber-{suffix}@test.local",
                GovernorateId = governorateId,
                AreaId = areaId,
                ApplicationUserId = user.Id
            },
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = subscriberId,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(10)
            });
        await context.SaveChangesAsync();

        var created = await requests.CreateForUserAsync(user.Id, bookId, copyId);
        Assert.True(created.IsSuccess, created.Error?.Message);
        return new PendingSeed(subscriberId, created.Value, copyId);
    }

    private static FormUrlEncodedContent BuildForm((string Key, string Value)[] fields, string token)
    {
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
        foreach (var field in fields)
            values[field.Key] = field.Value;
        return new FormUrlEncodedContent(values);
    }

    private static string ExtractToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, "No antiforgery token found");
        return match.Groups[1].Value;
    }

    private sealed record RoleUser(string UserId, string Name, Guid AreaId, Guid GovernorateId);

    private sealed record PendingSeed(Guid SubscriberId, Guid RequestId, Guid CopyId);
}
