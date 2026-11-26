using System.Threading.RateLimiting;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FreeBook.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"FreeBookTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Tests must not depend on a SQL Server instance: CI runs on Linux where
        // LocalDB does not exist, and Hangfire's SQL storage is initialized on startup.
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "false",
                ["Startup:MigrateOnStartup"] = "false",
                ["Startup:SeedAdmin"] = "false",
                ["Authentication:Google:ClientId"] = "test-client-id",
                ["Authentication:Google:ClientSecret"] = "test-client-secret"
            }));
        builder.UseSetting("Hangfire:Enabled", "false");
        builder.UseSetting("Startup:MigrateOnStartup", "false");
        builder.UseSetting("Startup:SeedAdmin", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<FreeBookDbContext>>();
            services.RemoveAll<DbContextOptions<FreeBookDbContext>>();
            services.RemoveAll<FreeBookDbContext>();
            services.AddDbContext<FreeBookDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, FakeGoogleAuthHandler>(
                    FakeGoogleAuthHandler.SchemeName, _ => { });

            // All WebAppTests share one TestServer, so the production 5/minute
            // login policy would reject later tests. Keep the middleware active
            // but give the test host a policy that cannot be exhausted.
            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
            services.Configure<RateLimiterOptions>(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddFixedWindowLimiter("login", limiter =>
                {
                    limiter.PermitLimit = 1000;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                    limiter.AutoReplenishment = true;
                });
            });
        });
    }
}
