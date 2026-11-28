using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class FreeBookDbContext : IdentityDbContext<ApplicationUser>
    {
        public FreeBookDbContext(DbContextOptions<FreeBookDbContext> options) : base(options)
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
            builder.Entity<Author>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<BookCategory>().Property(x => x.BookId).HasDefaultValueSql("(newid())");
            builder.Entity<BookCategory>().Property(x => x.CategoryId).HasDefaultValueSql("(newid())");
            builder.Entity<BookCopy>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<Rental>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<RentalCopy>().Property(x => x.RentalId).HasDefaultValueSql("(newid())");
            builder.Entity<RentalCopy>().Property(x => x.BookCopyId).HasDefaultValueSql("(newid())");
            builder.Entity<Governorate>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<Area>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<Subscriber>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<Subscription>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<Notification>().Property(x => x.Id).HasDefaultValueSql("(newid())");
            builder.Entity<RentalRequest>().Property(x => x.Id).HasDefaultValueSql("(newid())");

            // One row per underlying event even when a recurring job runs daily.
            builder.Entity<Notification>()
                .HasIndex(x => x.DedupeKey)
                .IsUnique();

            builder.Entity<Notification>()
                .HasIndex(x => new { x.IsRead, x.CreatedOn });

            builder.Entity<VwUser>().HasNoKey().ToView("VwUsers");

            // Composite keys
            builder.Entity<BookCategory>()
                .HasKey(bc => new { bc.BookId, bc.CategoryId });

            builder.Entity<RentalCopy>()
                .HasKey(rc => new { rc.RentalId, rc.BookCopyId });

            // Relationships
            builder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookCategory>()
                .HasOne(bc => bc.Book)
                .WithMany(b => b.BookCategories)
                .HasForeignKey(bc => bc.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BookCategory>()
                .HasOne(bc => bc.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(bc => bc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BookCopy>()
                .HasOne(bc => bc.Book)
                .WithMany(b => b.Copies)
                .HasForeignKey(bc => bc.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RentalCopy>()
                .HasOne(rc => rc.Rental)
                .WithMany(r => r.RentalCopies)
                .HasForeignKey(rc => rc.RentalId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RentalCopy>()
                .HasOne(rc => rc.BookCopy)
                .WithMany(bc => bc.Rentals)
                .HasForeignKey(rc => rc.BookCopyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rental>()
                .HasOne(r => r.Subscriber)
                .WithMany(s => s.Rentals)
                .HasForeignKey(r => r.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Area>()
                .HasOne(a => a.Governorate)
                .WithMany(g => g.Areas)
                .HasForeignKey(a => a.GovernorateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Subscriber>()
                .HasOne(s => s.Area)
                .WithMany(a => a.Subscribers)
                .HasForeignKey(s => s.AreaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Subscriber>()
                .HasOne(s => s.Governorate)
                .WithMany(g => g.Subscribers)
                .HasForeignKey(s => s.GovernorateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Existing rows predate the column, so let SQL Server stamp them on migration.
            builder.Entity<Subscriber>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<Subscription>()
                .HasOne(s => s.Subscriber)
                .WithMany(s => s.Subscriptions)
                .HasForeignKey(s => s.SubscriberId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional login-to-membership link. One Identity account can belong
            // to at most one subscriber; NULL is allowed for legacy rows.
            builder.Entity<Subscriber>()
                .Property(s => s.ApplicationUserId)
                .HasMaxLength(450);

            builder.Entity<Subscriber>()
                .HasIndex(s => s.ApplicationUserId)
                .IsUnique()
                .HasFilter("[ApplicationUserId] IS NOT NULL");

            builder.Entity<Subscriber>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RentalRequest>()
                .HasOne(r => r.Subscriber)
                .WithMany()
                .HasForeignKey(r => r.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RentalRequest>()
                .HasOne(r => r.Book)
                .WithMany()
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RentalRequest>()
                .HasOne(r => r.BookCopy)
                .WithMany()
                .HasForeignKey(r => r.BookCopyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RentalRequest>()
                .HasIndex(r => new { r.Status, r.CreatedOn });

            builder.Entity<RentalRequest>()
                .HasIndex(r => new { r.SubscriberId, r.BookId, r.Status });
        }

        public DbSet<Category> categories { get; set; }
        public DbSet<LogCategory> Logcategories { get; set; }
        public DbSet<SubCategory> Subcategories { get; set; }
        public DbSet<LogSubCategory> logSubCategories { get; set; }
        public DbSet<Book> books { get; set; }
        public DbSet<LogBook> logBooks { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<BookCategory> BookCategories { get; set; }
        public DbSet<BookCopy> BookCopies { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<RentalCopy> RentalCopies { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RentalRequest> RentalRequests { get; set; }
        public DbSet<VwUser> VwUsers { get; set; }
    }
}
