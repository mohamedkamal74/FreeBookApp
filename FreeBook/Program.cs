using System.Globalization;
using System.Threading.RateLimiting;
using Domin.Entity;
using FreeBook.Data;
using FreeBook.Jobs;
using FreeBook.Middleware;
using Hangfire;
using Infrastructure.Common.Configuration;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.IRepository.ServiceRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

    builder.Services.AddControllersWithViews()
        .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
        .AddDataAnnotationsLocalization();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("login", limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            limiter.AutoReplenishment = true;
        });
    });
    builder.Services.AddHealthChecks().AddDbContextCheck<FreeBookDbContext>("database");

    builder.Services.AddSession();
    builder.Services.AddDbContext<FreeBookDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BookConnection")));
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<FreeBookDbContext>();
    builder.Services.AddRazorPages();
    builder.Services.Configure<IdentityOptions>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredUniqueChars = 1;
    });

    builder.Services.AddScoped<IServiceRepository<Category>, ServicesCategory>();
    builder.Services.AddScoped<IServiceRepositoryLog<LogCategory>, ServiceLogCategory>();

    builder.Services.AddScoped<Infrastructure.IRepository.IAuthorRepository, Infrastructure.IRepository.ServiceRepository.ServicesAuthor>();
    builder.Services.AddScoped<IServiceRepository<Book>, Infrastructure.IRepository.ServiceRepository.ServicesBook>();
    builder.Services.AddScoped<IServiceRepository<BookCopy>, Infrastructure.IRepository.ServiceRepository.ServicesBookCopy>();
    builder.Services.AddScoped<Infrastructure.IRepository.IRentalRepository, Infrastructure.IRepository.ServiceRepository.ServicesRental>();
    builder.Services.AddScoped<IServiceRepository<Subscriber>, Infrastructure.IRepository.ServiceRepository.ServicesSubscriber>();
    builder.Services.AddScoped<Infrastructure.IRepository.ISubscriptionRepository, Infrastructure.IRepository.ServiceRepository.ServicesSubscription>();
    builder.Services.AddScoped<IServiceRepository<Governorate>, Infrastructure.IRepository.ServiceRepository.ServicesGovernorate>();
    builder.Services.AddScoped<IServiceRepository<Area>, Infrastructure.IRepository.ServiceRepository.ServicesArea>();

    // Core services
    builder.Services.AddScoped<Infrastructure.Common.Interfaces.IEmailService, Infrastructure.Services.EmailService>();
    builder.Services.AddScoped<Infrastructure.Common.Interfaces.IImageService, Infrastructure.Services.CloudinaryImageService>();
    builder.Services.AddScoped<Infrastructure.Common.Interfaces.ISmsService, Infrastructure.Services.TwilioSmsService>();

    // Background jobs
    builder.Services.AddScoped<FreeBook.Jobs.FineCalculationJob>();
    builder.Services.AddScoped<FreeBook.Jobs.ExpiryCheckJob>();
    builder.Services.AddScoped<FreeBook.Jobs.OverdueNotificationJob>();
    builder.Services.AddScoped<FreeBook.Jobs.CleanupJob>();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Admin";
        options.AccessDeniedPath = "/Admin/Home/Denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

    // Configuration binding
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
    builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
    builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));
    builder.Services.Configure<FineSettings>(builder.Configuration.GetSection("FineConfig"));

    // Localization configuration
    var localizationSection = builder.Configuration.GetSection("Localization");
    var defaultCulture = localizationSection["DefaultCulture"] ?? "ar";
    var supportedCultures = localizationSection.GetSection("SupportedCultures").Get<string[]>() ?? new[] { "ar", "en" };
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture(defaultCulture);
        options.SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
        options.SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
        options.RequestCultureProviders = new List<IRequestCultureProvider>
        {
        new CookieRequestCultureProvider(),
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
        };
    });

    // Hangfire configuration
    builder.Services.AddHangfire(config =>
        config.UseSqlServerStorage(builder.Configuration.GetConnectionString("BookConnection"),
            new Hangfire.SqlServer.SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

    builder.Services.AddHangfireServer(options =>
    {
        options.ServerName = "Bookify.Server";
        options.WorkerCount = Environment.ProcessorCount;
        options.Queues = new[] { "default", "emails", "notifications", "fines", "reports" };
    });


    var app = builder.Build();

    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler();
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    else
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRequestLocalization();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

    // Hangfire Dashboard
    var hangfirePath = app.Configuration["Hangfire:DashboardPath"] ?? "/hangfire";
    app.UseHangfireDashboard(hangfirePath, new DashboardOptions
    {
        Authorization = new[] { new HangfireAdminAuthorizationFilter() },
        DashboardTitle = "Bookify Background Jobs",
        StatsPollingInterval = 30000
    });

    app.MapRazorPages();
    app.MapHealthChecks("/health");


    app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllerRoute(
                              name: "areas",
                              pattern: "{area:exists}/{controller=Accounts}/{action=Login}/{id?}"
                            );
                        });

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    if (app.Configuration.GetValue<bool>("Startup:MigrateOnStartup")
        || app.Configuration.GetValue<bool>("Startup:SeedAdmin"))
    {
        await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, app.Logger);
    }

    // Schedule recurring jobs
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobManager.AddOrUpdate<FineCalculationJob>("daily-fines", x => x.CalculateDailyFinesAsync(), Cron.Daily(2, 0), TimeZoneInfo.Local, "fines");
        recurringJobManager.AddOrUpdate<ExpiryCheckJob>("expiry-check", x => x.CheckExpiringSubscriptionsAsync(), Cron.Daily(8, 0), TimeZoneInfo.Local, "notifications");
        recurringJobManager.AddOrUpdate<OverdueNotificationJob>("overdue-notifications", x => x.SendOverdueNotificationsAsync(), Cron.Daily(9, 0), TimeZoneInfo.Local, "notifications");
        recurringJobManager.AddOrUpdate<CleanupJob>("cleanup-old-jobs", x => x.CleanupAsync(), Cron.Weekly(DayOfWeek.Sunday, 3, 0), TimeZoneInfo.Local, "default");
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
