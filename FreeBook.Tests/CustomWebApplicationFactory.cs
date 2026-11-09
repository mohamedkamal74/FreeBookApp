using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                ["Startup:SeedAdmin"] = "false"
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
        });
    }
}
