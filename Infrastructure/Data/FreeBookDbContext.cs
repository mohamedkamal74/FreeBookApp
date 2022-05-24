using Domin.Entity;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class FreeBookDbContext:IdentityDbContext<ApplicationUser>
      
    {
        public FreeBookDbContext(DbContextOptions<FreeBookDbContext>options):base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //builder.Entity<IdentityUser>().ToTable("User", "Identity");
            //builder.Entity<IdentityRole>().ToTable("Role", "Identity");
            //builder.Entity<IdentityUserRole<string>>().ToTable("UserRole", "Identity");
            //builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaim", "Identity");
            //builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogin", "Identity");
            //builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaim", "Identity");
            //builder.Entity<IdentityUserToken<string>>().ToTable("UserToken", "Identity");

            builder.Entity<Category>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<LogCategory>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<SubCategory>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<LogSubCategory>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<Book>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<LogBook>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<VwUser>().HasNoKey().ToView("VwUsers");

        }
        public DbSet<Category> categories { get; set; }
        public DbSet<LogCategory> Logcategories { get; set; }
        public DbSet<SubCategory> Subcategories { get; set; }
        public DbSet<LogSubCategory> logSubCategories { get; set; }
        public DbSet<Book> books { get; set; }
        public DbSet<LogBook> logBooks { get; set; }
        public DbSet<VwUser> VwUsers { get; set; }

    }
}
