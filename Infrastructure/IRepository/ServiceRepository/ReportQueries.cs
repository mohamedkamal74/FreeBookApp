using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    /// <summary>
    /// Read-only EF projections for the Reports area. Every query materializes a scalar
    /// report DTO; no entity instances are returned to the web layer.
    /// </summary>
    internal static class ReportQueries
    {
        internal static IQueryable<OverdueBookReport> OverdueBooks(FreeBookDbContext context, DateTime now) =>
            from rental in context.Rentals.AsNoTracking()
            from copy in rental.RentalCopies
            where copy.ReturnDate == null && copy.EndDate < now
            orderby rental.StartDate, copy.RentalDate
            select new OverdueBookReport
            {
                SubscriberName = rental.Subscriber != null
                    ? rental.Subscriber.FirstName + " " + rental.Subscriber.LastName
                    : null,
                BookName = copy.BookCopy != null && copy.BookCopy.Book != null
                    ? copy.BookCopy.Book.Name
                    : null,
                RentalDate = copy.RentalDate,
                EndDate = copy.EndDate,
                PenaltyPaid = rental.PenaltyPaid
            };

        internal static IQueryable<PopularBookReport> PopularBooks(FreeBookDbContext context) =>
            from rentalCopy in context.RentalCopies.AsNoTracking()
            join bookCopy in context.BookCopies.AsNoTracking() on rentalCopy.BookCopyId equals bookCopy.Id
            join book in context.books.AsNoTracking() on bookCopy.BookId equals book.Id
            group book by new
            {
                book.Id,
                book.Name,
                AuthorName = book.Author != null ? book.Author.Name : null,
                CategoryName = book.Category != null ? book.Category.Name : null
            }
            into grouped
            select new PopularBookReport
            {
                BookId = grouped.Key.Id,
                BookName = grouped.Key.Name,
                AuthorName = grouped.Key.AuthorName,
                CategoryName = grouped.Key.CategoryName,
                RentalCount = grouped.Count()
            };

        internal static IQueryable<ActiveSubscriberReport> ActiveSubscribers(FreeBookDbContext context, DateTime now) =>
            from subscriber in context.Subscribers.AsNoTracking()
            where subscriber.Subscriptions.Any(subscription =>
                subscription.StartDate <= now && subscription.EndDate >= now)
            orderby subscriber.FirstName, subscriber.LastName
            select new ActiveSubscriberReport
            {
                Id = subscriber.Id,
                FullName = subscriber.FirstName + " " + subscriber.LastName,
                NationalId = subscriber.NationalId,
                MobileNumber = subscriber.MobileNumber,
                Email = subscriber.Email,
                AreaName = subscriber.Area != null ? subscriber.Area.Name : null,
                GovernorateName = subscriber.Governorate != null ? subscriber.Governorate.Name : null,
                ActiveSubscriptionEndDate = subscriber.Subscriptions
                    .Where(subscription => subscription.StartDate <= now && subscription.EndDate >= now)
                    .Select(subscription => (DateTime?)subscription.EndDate)
                    .Max()
            };

        internal static IQueryable<ExpiringSubscriptionReport> ExpiringSubscriptions(
            FreeBookDbContext context,
            DateTime now,
            DateTime future) =>
            from subscription in context.Subscriptions.AsNoTracking()
            where subscription.EndDate >= now && subscription.EndDate <= future
            orderby subscription.EndDate
            select new ExpiringSubscriptionReport
            {
                SubscriberName = subscription.Subscriber != null
                    ? subscription.Subscriber.FirstName + " " + subscription.Subscriber.LastName
                    : null,
                NationalId = subscription.Subscriber != null ? subscription.Subscriber.NationalId : null,
                MobileNumber = subscription.Subscriber != null ? subscription.Subscriber.MobileNumber : null,
                Email = subscription.Subscriber != null ? subscription.Subscriber.Email : null,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate
            };

        internal static IQueryable<RentalHistoryReportRow> RentalHistoryRows(
            FreeBookDbContext context,
            Guid? subscriberId) =>
            from rental in context.Rentals.AsNoTracking()
            where !subscriberId.HasValue || rental.SubscriberId == subscriberId.Value
            from copy in rental.RentalCopies
            orderby rental.StartDate descending, copy.RentalDate
            select new RentalHistoryReportRow
            {
                RentalId = rental.Id,
                SubscriberName = rental.Subscriber != null
                    ? rental.Subscriber.FirstName + " " + rental.Subscriber.LastName
                    : null,
                BookName = copy.BookCopy != null && copy.BookCopy.Book != null
                    ? copy.BookCopy.Book.Name
                    : null,
                RentalDate = copy.RentalDate,
                EndDate = copy.EndDate,
                ReturnDate = copy.ReturnDate
            };

        internal static IQueryable<ReportFilterOption> RentalHistoryOptions(FreeBookDbContext context) =>
            from subscriber in context.Subscribers.AsNoTracking()
            orderby subscriber.FirstName, subscriber.LastName
            select new ReportFilterOption
            {
                Id = subscriber.Id,
                Name = subscriber.FirstName + " " + subscriber.LastName
            };

        internal static IQueryable<BooksByCategoryReport> BooksByCategoryRows(FreeBookDbContext context) =>
            from book in context.books.AsNoTracking()
                .Where(book => book.CurrentState > 0 && book.CategoryId.HasValue)
            join category in context.categories.AsNoTracking()
                .Where(category => category.Currentstste > 0)
                on book.CategoryId equals category.Id
            group book by new { category.Id, category.Name } into grouped
            select new BooksByCategoryReport
            {
                CategoryName = grouped.Key.Name,
                Count = grouped.Count()
            };

        internal static IQueryable<BooksByAuthorReport> BooksByAuthorRows(FreeBookDbContext context) =>
            from book in context.books.AsNoTracking()
                .Where(book => book.CurrentState > 0 && book.AuthorId != Guid.Empty)
            join author in context.Authors.AsNoTracking()
                .Where(author => !author.IsDeleted)
                on book.AuthorId equals author.Id
            group book by new { author.Id, author.Name } into grouped
            select new BooksByAuthorReport
            {
                AuthorName = grouped.Key.Name,
                Count = grouped.Count()
            };

        internal static IQueryable<BookInventoryReport> BookInventoryRows(
            FreeBookDbContext context,
            BookInventoryQuery query) =>
            from book in context.books.AsNoTracking()
                .Where(book => book.CurrentState > 0)
                .Where(book => !query.AuthorId.HasValue || book.AuthorId == query.AuthorId.Value)
                .Where(book => !query.CategoryId.HasValue || book.CategoryId == query.CategoryId.Value)
                .Where(book => !query.AvailableOnly || book.IsAvailableForRental)
            select new BookInventoryReport
            {
                BookId = book.Id,
                BookName = book.Name,
                ImageThumbnailUrl = book.ImageThumbnailUrl,
                ImageUrl = book.ImageUrl,
                AuthorName = book.Author != null ? book.Author.Name : null,
                CategoryName = book.Category != null ? book.Category.Name : null,
                SubCategoryName = book.SubCategory != null ? book.SubCategory.Name : null,
                Publisher = book.Publisher,
                Hall = book.Hall,
                IsAvailableForRental = book.IsAvailableForRental,
                CopyCount = context.BookCopies.AsNoTracking().Count(copy => copy.BookId == book.Id),
                AvailableCopyCount = context.BookCopies.AsNoTracking()
                    .Count(copy => copy.BookId == book.Id && copy.IsAvailableForRental),
                RentalCount = context.RentalCopies.AsNoTracking()
                    .Count(copy => copy.BookCopy != null && copy.BookCopy.BookId == book.Id)
            };

        internal static IQueryable<ReportFilterOption> AuthorOptions(FreeBookDbContext context) =>
            context.Authors.AsNoTracking()
                .Where(author => !author.IsDeleted)
                .OrderBy(author => author.Name)
                .Select(author => new ReportFilterOption
                {
                    Id = author.Id,
                    Name = author.Name
                });

        internal static IQueryable<ReportFilterOption> CategoryOptions(FreeBookDbContext context) =>
            context.categories.AsNoTracking()
                .Where(category => category.Currentstste > 0)
                .OrderBy(category => category.Name)
                .Select(category => new ReportFilterOption
                {
                    Id = category.Id,
                    Name = category.Name
                });
    }
}
