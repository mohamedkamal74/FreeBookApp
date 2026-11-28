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

public class MemberSelfServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Admin@12345";
    private const string AdminEmail = "member-ui-admin@test.local";

    private readonly CustomWebApplicationFactory _factory;

    public MemberSelfServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MemberPages_RequireAuthenticatedUserRole()
    {
        var anonymousClient = CreateClient();
        var anonymousResponse = await anonymousClient.GetAsync("/Member/Orders?culture=en");

        Assert.Equal(HttpStatusCode.Redirect, anonymousResponse.StatusCode);
        Assert.Contains("/Admin/Accounts/Login", anonymousResponse.Headers.Location!.ToString());

        await EnsureAdminAsync();
        var adminClient = await LoginAsync(AdminEmail);
        var deniedResponse = await adminClient.GetAsync("/Member/Orders?culture=en");

        Assert.Equal(HttpStatusCode.Redirect, deniedResponse.StatusCode);
        Assert.Contains("/Admin/Home/Denied", deniedResponse.Headers.Location!.ToString());
    }

    [Fact]
    public async Task OrdersPage_ListsAvailableBooksAndMemberNavigation()
    {
        var seed = await SeedMemberAsync();
        var client = await LoginAsync(seed.Email);

        var page = await client.GetStringAsync("/Member/Orders?culture=en");

        Assert.Contains("Order Books", page);
        Assert.Contains("My Rentals", page);
        Assert.Contains(seed.BookTitle, page);
        Assert.Contains($"id=\"orderBook_{seed.BookId}\"", page);
        Assert.Contains("id=\"myRequests\"", page);
    }

    [Fact]
    public async Task RequestBook_CreatesPendingRequestAndBlocksDuplicate()
    {
        var seed = await SeedMemberAsync();
        var client = await LoginAsync(seed.Email);
        var page = await client.GetStringAsync("/Member/Orders?culture=en");

        var response = await client.PostAsync("/Member/RequestBook?culture=en", BuildForm(new[]
        {
            ("BookId", seed.BookId.ToString())
        }, ExtractToken(page)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var requestId = await FindRequestIdAsync(seed.UserId, seed.BookId);
        Assert.NotEqual(Guid.Empty, requestId);

        var afterRequest = await client.GetStringAsync("/Member/Orders?culture=en");
        Assert.Contains("Rental request sent for admin approval", afterRequest);
        Assert.Contains($"id=\"cancelRequest_{requestId}\"", afterRequest);

        var duplicate = await client.PostAsync("/Member/RequestBook?culture=en", BuildForm(new[]
        {
            ("BookId", seed.BookId.ToString())
        }, ExtractToken(afterRequest)));
        Assert.Equal(HttpStatusCode.Redirect, duplicate.StatusCode);

        var afterDuplicate = await client.GetStringAsync("/Member/Orders?culture=en");
        Assert.Contains("A pending request already exists for this book", afterDuplicate);
    }

    [Fact]
    public async Task RequestBook_ForUnlinkedUser_ShowsLocalizedError()
    {
        var seed = await SeedMemberAsync(linked: false);
        var client = await LoginAsync(seed.Email);
        var page = await client.GetStringAsync("/Member/Orders?culture=en");

        var response = await client.PostAsync("/Member/RequestBook?culture=en", BuildForm(new[]
        {
            ("BookId", seed.BookId.ToString())
        }, ExtractToken(page)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var after = await client.GetStringAsync("/Member/Orders?culture=en");
        Assert.Contains("Your account is not linked to a library membership with an active subscription", after);
    }

    [Fact]
    public async Task MyRentals_ShowsApprovedRentalWithReturnAndExtendControls()
    {
        var seed = await SeedMemberAsync();
        var client = await LoginAsync(seed.Email);
        var (_, rentalId, _) = await CreateAndApproveRequestAsync(seed);

        var page = await client.GetStringAsync("/Member/MyRentals?culture=en");

        Assert.Contains("My Rentals", page);
        Assert.Contains(seed.BookTitle, page);
        Assert.Contains($"id=\"returnCopy_{rentalId}_{seed.CopyId}\"", page);
        Assert.Contains($"id=\"extendCopy_{rentalId}_{seed.CopyId}\"", page);
    }

    [Fact]
    public async Task ExtendAndReturnActions_UpdateRentalAndShowLocalizedFlash()
    {
        var seed = await SeedMemberAsync();
        var client = await LoginAsync(seed.Email);
        var (_, rentalId, rentalDate) = await CreateAndApproveRequestAsync(seed);

        var page = await client.GetStringAsync("/Member/MyRentals?culture=en");
        var requestedEndDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        var extendResponse = await client.PostAsync("/Member/ExtendCopy?culture=en", BuildForm(new[]
        {
            ("RentalId", rentalId.ToString()),
            ("BookCopyId", seed.CopyId.ToString()),
            ("NewEndDate", requestedEndDate)
        }, ExtractToken(page)));
        Assert.Equal(HttpStatusCode.Redirect, extendResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();
            var rentalCopy = await context.RentalCopies.SingleAsync(x => x.BookCopyId == seed.CopyId);
            Assert.Equal(rentalDate.AddDays((int)RentalsConfigurations.MaxRentalDuration), rentalCopy.EndDate);
            Assert.NotNull(rentalCopy.ExtendedOn);
        }

        var afterExtend = await client.GetStringAsync("/Member/MyRentals?culture=en");
        Assert.Contains("Rental extended successfully", afterExtend);

        var returnResponse = await client.PostAsync("/Member/ReturnCopy?culture=en", BuildForm(new[]
        {
            ("RentalId", rentalId.ToString()),
            ("BookCopyId", seed.CopyId.ToString())
        }, ExtractToken(afterExtend)));
        Assert.Equal(HttpStatusCode.Redirect, returnResponse.StatusCode);

        var afterReturn = await client.GetStringAsync("/Member/MyRentals?culture=en");
        Assert.Contains("Book returned successfully", afterReturn);
        Assert.DoesNotContain($"id=\"returnCopy_{rentalId}_{seed.CopyId}\"", afterReturn);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();
            Assert.NotNull((await context.RentalCopies.SingleAsync(x => x.BookCopyId == seed.CopyId)).ReturnDate);
            Assert.True((await context.BookCopies.SingleAsync(x => x.Id == seed.CopyId)).IsAvailableForRental);
        }
    }

    [Fact]
    public async Task MemberActions_CannotMutateAnotherUsersRental()
    {
        var owner = await SeedMemberAsync();
        var other = await SeedMemberAsync();
        var (_, ownerRentalId, _) = await CreateAndApproveRequestAsync(owner);

        var otherClient = await LoginAsync(other.Email);
        var page = await otherClient.GetStringAsync("/Member/MyRentals?culture=en");
        var response = await otherClient.PostAsync("/Member/ReturnCopy?culture=en", BuildForm(new[]
        {
            ("RentalId", ownerRentalId.ToString()),
            ("BookCopyId", owner.CopyId.ToString())
        }, ExtractToken(page)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var after = await otherClient.GetStringAsync("/Member/MyRentals?culture=en");
        Assert.Contains("Rental not found", after);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();
            var rentalCopy = await context.RentalCopies.SingleAsync(x => x.BookCopyId == owner.CopyId);
            Assert.Null(rentalCopy.ReturnDate);
        }
    }

    [Fact]
    public async Task MemberPages_AreLocalizedInArabic()
    {
        var seed = await SeedMemberAsync();
        var client = await LoginAsync(seed.Email);
        await CreateAndApproveRequestAsync(seed);
        var english = await client.GetStringAsync("/Member/Orders?culture=en");

        var cultureResponse = await client.PostAsync("/Localization/SetCulture", BuildForm(new[]
        {
            ("culture", "ar"),
            ("returnUrl", "/Member/Orders")
        }, ExtractToken(english)));
        Assert.Equal(HttpStatusCode.Redirect, cultureResponse.StatusCode);

        var arabicOrders = WebUtility.HtmlDecode(await client.GetStringAsync(cultureResponse.Headers.Location!));
        Assert.Contains("طلب الكتب", arabicOrders);
        Assert.Contains("طلباتي", arabicOrders);
        Assert.Contains("طلب الكتاب", arabicOrders);

        var arabicRentals = WebUtility.HtmlDecode(await client.GetStringAsync("/Member/MyRentals"));
        Assert.Contains("استعاراتي", arabicRentals);
        Assert.Contains("الإجراءات", arabicRentals);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<HttpClient> LoginAsync(string email)
    {
        var client = CreateClient();
        var token = ExtractToken(await client.GetStringAsync("/Admin?culture=en"));
        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en", BuildForm(new[]
        {
            ("Email", email),
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
                Name = "Member UI Admin",
                ActiveUser = true,
                EmailConfirmed = true,
                ImageUser = string.Empty
            };
            Assert.True((await userManager.CreateAsync(admin, Password)).Succeeded);
            Assert.True((await userManager.AddToRoleAsync(admin, "Admin")).Succeeded);
        }
    }

    private async Task<MemberSeed> SeedMemberAsync(bool linked = true, bool active = true)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"member-ui-{suffix}@test.local";
        var userId = $"member-ui-{suffix}";
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();

        if (!await roleManager.RoleExistsAsync("User"))
            Assert.True((await roleManager.CreateAsync(new IdentityRole("User"))).Succeeded);

        var user = new ApplicationUser
        {
            Id = userId,
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
        var bookId = Guid.NewGuid();
        var copyId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        // PublicHomeQueries orders by name and takes 24 rows. A numeric prefix keeps
        // this fixture first even when other tests have populated the shared InMemory DB.
        var bookTitle = $"000 Orderable Book {suffix}";

        context.AddRange(
            new Governorate { Id = governorateId, Name = $"UI Member Gov {suffix}" },
            new Area { Id = areaId, Name = $"UI Member Area {suffix}", GovernorateId = governorateId },
            new Author { Id = authorId, Name = $"UI Member Author {suffix}" },
            new Category { Id = categoryId, Name = $"UI Member Category {suffix}" },
            new Book
            {
                Id = bookId,
                Name = bookTitle,
                AuthorId = authorId,
                CategoryId = categoryId,
                CurrentState = (int)Helper.ECurrentState.Active,
                IsAvailableForRental = true
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
                FirstName = "UI",
                LastName = suffix,
                DateOfBirth = now.AddYears(-30),
                NationalId = $"34{suffix}",
                MobileNumber = $"011{suffix}",
                Email = $"subscriber-{suffix}@test.local",
                GovernorateId = governorateId,
                AreaId = areaId,
                ApplicationUserId = linked ? userId : null
            },
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = subscriberId,
                StartDate = now.AddDays(-1),
                EndDate = active ? now.AddDays(10) : now.AddDays(-1)
            });
        await context.SaveChangesAsync();

        return new MemberSeed(email, userId, bookId, copyId, bookTitle);
    }

    private async Task<Guid> FindRequestIdAsync(string userId, Guid bookId)
    {
        using var scope = _factory.Services.CreateScope();
        var requests = scope.ServiceProvider.GetRequiredService<IRentalRequestRepository>();
        var result = await requests.GetByApplicationUserIdAsync(userId);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value!.First(x => x.BookId == bookId).Id;
    }

    private async Task<(Guid RequestId, Guid RentalId, DateTime RentalDate)> CreateAndApproveRequestAsync(MemberSeed seed)
    {
        using var scope = _factory.Services.CreateScope();
        var requests = scope.ServiceProvider.GetRequiredService<IRentalRequestRepository>();
        var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.FreeBookDbContext>();

        var created = await requests.CreateForUserAsync(seed.UserId, seed.BookId, seed.CopyId);
        Assert.True(created.IsSuccess, created.Error?.Message);

        var approved = await requests.ApproveAsync(created.Value, new[] { seed.CopyId }, "member-ui-approver");
        Assert.True(approved.IsSuccess, approved.Error?.Message);

        var rentalId = approved.Value!.Value;
        var rentalDate = (await context.RentalCopies.SingleAsync(x => x.BookCopyId == seed.CopyId)).RentalDate;
        return (created.Value, rentalId, rentalDate);
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

    private sealed record MemberSeed(
        string Email,
        string UserId,
        Guid BookId,
        Guid CopyId,
        string BookTitle);
}
