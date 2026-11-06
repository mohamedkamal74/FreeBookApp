using System.Globalization;
using System.Threading.RateLimiting;
using Domin.Entity;
using FreeBook.Data;
using FreeBook.Middleware;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.IRepository.ServiceRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();

public partial class Program { }
