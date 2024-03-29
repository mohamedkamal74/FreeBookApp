        using Domin.Entity;
        using Infrastructure.Data;
        using Infrastructure.IRepository;
        using Infrastructure.ViewModel;
        using Microsoft.AspNetCore.Identity;
        using Microsoft.EntityFrameworkCore;
        using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
        using Infrastructure.IRepository.ServiceRepository;

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddSession();
        builder.Services.AddDbContext<FreeBookDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("BookConnection")));
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<FreeBookDbContext>();
        builder.Services.AddRazorPages();
        builder.Services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = false;
            options.Password.RequiredUniqueChars = 0;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 5;

        });

        builder.Services.AddScoped<IServiceRepository<Category>, ServicesCategory>();
        builder.Services.AddScoped<IServiceRepositoryLog<LogCategory>, ServiceLogCategory>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Admin";
            options.AccessDeniedPath = "/Admin/Home/Denied";
        });


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRazorPages();


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

        app.Run();
