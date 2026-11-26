using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Domin.Entity;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class WebAppTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@test.local";
    private const string AdminPassword = "Admin@12345";
    private const string UserEmail = "user@test.local";
    private const string UserPassword = "User@12345";
    private readonly CustomWebApplicationFactory _factory;

    public WebAppTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomePage_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousAdminRequest_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Admin/Accounts/Roles");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task HomePage_IncludesSecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task AnonymousCategoriesRequest_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Admin/Categories/Categories");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsServedInDevelopment()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_ThenProtectedRegisterPage_Loads()
    {
        await SeedAdminAsync();
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPage = await client.GetAsync("/Admin");
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(token));

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = AdminEmail,
            ["Password"] = AdminPassword,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token!
        });
        var loginResponse = await client.PostAsync("/Admin/Accounts/Login", form);
        Assert.True(
            loginResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Unexpected login status: {loginResponse.StatusCode}");

        var registerPage = await client.GetAsync("/Admin/Accounts/Register");
        Assert.Equal(HttpStatusCode.OK, registerPage.StatusCode);
    }

    [Fact]
    public async Task PasswordLogin_SendsAdminToDashboard()
    {
        await SeedAdminAsync();
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var loginPage = await client.GetAsync("/Admin/Accounts/Login?culture=en");
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());

        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = AdminEmail,
                ["Password"] = AdminPassword,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token!
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Dashboard/Index", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task PasswordLogin_SendsUserToPublicHome()
    {
        await SeedUserAsync(UserEmail, UserPassword, "User");
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var loginPage = await client.GetAsync("/Admin/Accounts/Login?culture=en");
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());

        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = UserEmail,
                ["Password"] = UserPassword,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token!
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.True(location is "/" or "/Home" || location.StartsWith("/?", StringComparison.Ordinal), location);
    }

    [Fact]
    public async Task AdminLayout_EmitsOneSharedJsStringBundle()
    {
        await SeedAdminAsync();
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPage = await client.GetAsync("/Admin?culture=en");
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = AdminEmail,
            ["Password"] = AdminPassword,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token!
        });
        await client.PostAsync("/Admin/Accounts/Login?culture=en", form);

        var html = await client.GetStringAsync("/Admin/Areas/Areas?culture=en");

        Assert.Single(Regex.Matches(html, "window\\.bookify"));
        Assert.Contains("\"search\":\"Search:\"", html);
        Assert.DoesNotContain("let dtSearch", html);
        Assert.DoesNotContain("initDataTablesLanguage();", html);
    }

    [Fact]
    public async Task BooksPage_LoadsForAdmin_WithExpectedHooks()
    {
        await SeedAdminAsync();
        Guid authorId;
        using (var scope = _factory.Services.CreateScope())
        {
            var authors = scope.ServiceProvider.GetRequiredService<IServiceRepository<AuthorDto, AuthorCommand>>();
            var authorName = $"Test Author {Guid.NewGuid():N}";
            Assert.True((await authors.SaveAsync(new AuthorCommand { Name = authorName })).IsSuccess);
            authorId = (await authors.FindByAsync(authorName)).Value!.Id;
        }

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginAsync(client);

        var bookName = $"Thumbnail Book {Guid.NewGuid():N}";
        var initialPage = await client.GetStringAsync("/Admin/Books/Books?culture=en");
        var createResponse = await client.PostAsync("/Admin/Books/Save?culture=en", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["NewBook.BookId"] = string.Empty,
                ["NewBook.Name"] = bookName,
                ["NewBook.AuthorId"] = authorId.ToString(),
                ["NewBook.ImageThumbnailUrl"] = "https://res.cloudinary.com/example/image/upload/thumb.png",
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(initialPage)!
            }));
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        var html = await client.GetStringAsync("/Admin/Books/Books?culture=en");

        Assert.Contains("id=\"tableBooks\"", html);
        Assert.Contains(bookName, html);
        Assert.Contains("book-cover-thumb", html);
        Assert.Contains("https://res.cloudinary.com/example/image/upload/thumb.png", html);
        Assert.Contains("fas fa-edit", html);
        Assert.Contains("fas fa-trash", html);
        Assert.Contains("aria-label=\"Edit\"", html);
        Assert.Contains("aria-label=\"Delete\"", html);
        Assert.DoesNotContain("value=\"Edit\"", html);
        Assert.DoesNotContain("value=\"Delete\"", html);
        Assert.Contains("id=\"bookModalBtnSave\"", html);
        Assert.Contains("id=\"coverImage\"", html);
        Assert.Contains("enctype=\"multipart/form-data\"", html);
        Assert.Contains("id=\"deleteForm\"", html);
        Assert.Contains("Add New Book", html);
        Assert.Contains("Cover Image", html);
        Assert.Contains("/Admin/Books/Save", html);
        Assert.Single(Regex.Matches(html, "window\\.bookify"));
    }

    [Fact]
    public async Task BooksSave_CreatesBookAndRejectsDuplicateName()
    {
        await SeedAdminAsync();
        Guid authorId;
        using (var scope = _factory.Services.CreateScope())
        {
            var authors = scope.ServiceProvider.GetRequiredService<IServiceRepository<AuthorDto, AuthorCommand>>();
            var authorName = $"Test Author {Guid.NewGuid():N}";
            Assert.True((await authors.SaveAsync(new AuthorCommand { Name = authorName })).IsSuccess);
            authorId = (await authors.FindByAsync(authorName)).Value!.Id;
        }

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginAsync(client);

        var bookName = $"Unique Book {Guid.NewGuid():N}";
        var page = await client.GetStringAsync("/Admin/Books/Books?culture=en");
        var createResponse = await client.PostAsync("/Admin/Books/Save?culture=en", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["NewBook.BookId"] = string.Empty,
                ["NewBook.Name"] = bookName,
                ["NewBook.AuthorId"] = authorId.ToString(),
                ["NewBook.Publisher"] = "Test Press",
                ["NewBook.PublishingDate"] = "2020-01-01",
                ["NewBook.Hall"] = "A1",
                ["NewBook.IsAvailableForRental"] = "true",
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(page)!
            }));
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        var afterCreate = await client.GetStringAsync("/Admin/Books/Books?culture=en");
        Assert.Contains(bookName, afterCreate);

        var duplicateResponse = await client.PostAsync("/Admin/Books/Save?culture=en", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["NewBook.BookId"] = string.Empty,
                ["NewBook.Name"] = bookName,
                ["NewBook.AuthorId"] = authorId.ToString(),
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(afterCreate)!
            }));
        Assert.Equal(HttpStatusCode.Redirect, duplicateResponse.StatusCode);

        var afterDuplicate = await client.GetStringAsync("/Admin/Books/Books?culture=en");
        Assert.Contains("This name already exists!", afterDuplicate);

        var afterFlashConsumed = await client.GetStringAsync("/Admin/Books/Books?culture=en");
        Assert.DoesNotContain("This name already exists!", afterFlashConsumed);
    }

    [Fact]
    public async Task AccountsFlashMessages_AreLocalizedAndConsumedOnce()
    {
        await SeedAdminAsync();
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginAsync(client);

        await CreateRoleAndAssertFlashAsync(client, "en", "User group saved successfully");
        await CreateRoleAndAssertFlashAsync(client, "ar", "تم حفظ مجموعة المستخدم بنجاح");
        await AssertInvalidImageFlashAsync(client, "en",
            "Only JPG, JPEG, PNG, GIF, or WEBP images up to 5 MB are allowed.");
        await AssertInvalidImageFlashAsync(client, "ar",
            "يُسمح فقط بصور JPG أو JPEG أو PNG أو GIF أو WEBP بحجم لا يتجاوز 5 ميغابايت.");
    }

    [Theory]
    [InlineData("en", "Sign in with Google")]
    [InlineData("ar", "تسجيل الدخول باستخدام جوجل")]
    public async Task LoginPage_ShowsLocalizedGoogleButton_WhenConfigured(string culture, string expectedText)
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync($"/Admin/Accounts/Login?culture={culture}");

        Assert.Contains("id=\"googleLogin\"", html);
        Assert.Contains(expectedText, WebUtility.HtmlDecode(html));
        Assert.Contains("/Admin/Accounts/ExternalLogin", html);
        Assert.Contains("btn-google-auth", html);
        Assert.Contains("<svg", html);
        Assert.DoesNotContain("fa-facebook", html);
        Assert.DoesNotContain("lbLoginWithFacebook", html);
    }

    [Theory]
    [InlineData("en", "Register a new membership")]
    [InlineData("ar", "تسجيل عضوية جديدة")]
    public async Task LoginPage_DoesNotShowRegistrationLink(string culture, string registrationText)
    {
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/Admin/Accounts/Login?culture={culture}"));

        Assert.DoesNotContain(registrationText, html);
        Assert.DoesNotContain("href=\"#\"", html);
    }

    [Fact]
    public async Task GoogleLogin_IsHiddenAndRejected_WithoutCredentials()
    {
        using var disabledFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Google:ClientId"] = string.Empty,
                    ["Authentication:Google:ClientSecret"] = string.Empty
                })));
        var client = disabledFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var html = await client.GetStringAsync("/Admin/Accounts/Login?culture=en");
        var response = await client.GetAsync("/Admin/Accounts/ExternalLogin?culture=en");

        Assert.DoesNotContain("id=\"googleLogin\"", html);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task GoogleLogin_CreatesUserWithUserRoleAndSignsIn()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var challenge = await client.GetAsync(
            "/Admin/Accounts/ExternalLogin?culture=en&returnUrl=%2FAdmin%2FAccounts%2FRoles");
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);

        var callbackUrl = challenge.Headers.Location!.OriginalString;
        Assert.Contains("/Admin/Accounts/ExternalLoginCallback", callbackUrl);

        var callback = await client.GetAsync(callbackUrl);
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/Admin/Accounts/Roles", callback.Headers.Location!.OriginalString);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var user = await userManager.FindByEmailAsync(FakeGoogleAuthHandler.Email);

            Assert.NotNull(user);
            Assert.Equal(FakeGoogleAuthHandler.DisplayName, user!.Name);
            Assert.True(user.EmailConfirmed);
            Assert.True(user.ActiveUser);
            Assert.True(await roleManager.RoleExistsAsync("User"));
            Assert.True(await userManager.IsInRoleAsync(user, "User"));
            Assert.False(await userManager.IsInRoleAsync(user, "Admin"));
        }

        var protectedPage = await client.GetAsync("/Admin/Accounts/Roles");
        Assert.Equal(HttpStatusCode.OK, protectedPage.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_WithoutReturnUrl_SendsUserHome()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var challenge = await client.GetAsync("/Admin/Accounts/ExternalLogin?culture=en");
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);

        var callback = await client.GetAsync(challenge.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);

        var location = callback.Headers.Location!.OriginalString;
        Assert.True(location is "/" or "/Home" || location.StartsWith("/?", StringComparison.Ordinal), location);
    }

    private static async Task CreateRoleAndAssertFlashAsync(HttpClient client, string culture, string expectedText)
    {
        var page = await client.GetStringAsync($"/Admin/Accounts/Roles?culture={culture}");
        var roleName = $"FlashRole-{Guid.NewGuid():N}";

        var response = await client.PostAsync($"/Admin/Accounts/Roles?culture={culture}", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["NewRole.RoleName"] = roleName,
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(page)!
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var serializedText = JsonSerializer.Serialize(expectedText);
        var afterSave = await client.GetStringAsync($"/Admin/Accounts/Roles?culture={culture}");
        Assert.Contains(roleName, afterSave);
        Assert.Contains(serializedText, afterSave);

        var afterFlashConsumed = await client.GetStringAsync($"/Admin/Accounts/Roles?culture={culture}");
        Assert.DoesNotContain(serializedText, afterFlashConsumed);
    }

    private static async Task AssertInvalidImageFlashAsync(HttpClient client, string culture, string expectedText)
    {
        var page = await client.GetStringAsync($"/Admin/Accounts/Register?culture={culture}");
        using var content = new MultipartFormDataContent
        {
            { new StringContent(ExtractAntiForgeryToken(page)!), "__RequestVerificationToken" },
            { new StringContent(string.Empty), "NewRegister.Id" },
            { new StringContent("Flash Probe"), "NewRegister.Name" },
            { new StringContent("flash-probe@example.com"), "NewRegister.Email" },
            { new StringContent("Admin"), "NewRegister.RoleName" },
            { new StringContent("Test@12345"), "NewRegister.Password" },
            { new StringContent("Test@12345"), "NewRegister.ConfirmPassword" },
            { new StringContent("true"), "NewRegister.ActiveUser" }
        };
        content.Add(new ByteArrayContent(new byte[] { (byte)'x' }), "NewRegister.ImageUser", "invalid.txt");

        var response = await client.PostAsync($"/Admin/Accounts/Register?culture={culture}", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var serializedText = JsonSerializer.Serialize(expectedText);
        var afterPost = await client.GetStringAsync($"/Admin/Accounts/Register?culture={culture}");
        Assert.Contains(serializedText, afterPost);

        var afterFlashConsumed = await client.GetStringAsync($"/Admin/Accounts/Register?culture={culture}");
        Assert.DoesNotContain(serializedText, afterFlashConsumed);
    }

    private async Task LoginAsync(HttpClient client)
    {
        var loginPage = await client.GetAsync("/Admin?culture=en");
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(token));

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = AdminEmail,
            ["Password"] = AdminPassword,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token!
        });
        var response = await client.PostAsync("/Admin/Accounts/Login?culture=en", form);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Unexpected login status: {response.StatusCode}");
    }

    private async Task SeedAdminAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                Name = "Administrator",
                ActiveUser = true,
                EmailConfirmed = true,
                ImageUser = string.Empty
            };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    private async Task SeedUserAsync(string email, string password, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = "Test User",
                ActiveUser = true,
                EmailConfirmed = true,
                ImageUser = string.Empty
            };
            var created = await userManager.CreateAsync(user, password);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static string? ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}
