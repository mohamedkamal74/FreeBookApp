using System.Net;
using Domin.Entity;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBook.Tests;

public class GoogleAdminRoleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GoogleAdminRoleTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GoogleLogin_ExistingAdmin_KeepsAdminRoleAndHonorsReturnUrl()
    {
        await SeedExternalAdminAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
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
            var user = await userManager.FindByEmailAsync(FakeGoogleAuthHandler.Email);

            Assert.NotNull(user);
            Assert.True(await userManager.IsInRoleAsync(user!, "Admin"));
            Assert.False(await userManager.IsInRoleAsync(user!, "User"));
        }

        var rolesPage = await client.GetAsync("/Admin/Accounts/Roles?culture=en");
        Assert.Equal(HttpStatusCode.OK, rolesPage.StatusCode);
    }

    private async Task SeedExternalAdminAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = FakeGoogleAuthHandler.Email,
            Email = FakeGoogleAuthHandler.Email,
            Name = "Existing Google Admin",
            ActiveUser = true,
            EmailConfirmed = true,
            ImageUser = string.Empty
        };

        var created = await userManager.CreateAsync(user);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
        Assert.True((await userManager.AddToRoleAsync(user, "Admin")).Succeeded);
    }
}
