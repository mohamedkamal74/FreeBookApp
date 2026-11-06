using System.Net;
using System.Text.RegularExpressions;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class WebAppTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@test.local";
    private const string AdminPassword = "Admin@12345";
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

    private static string? ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}
