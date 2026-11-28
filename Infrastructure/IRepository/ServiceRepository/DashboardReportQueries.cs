using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    /// <summary>
    /// Reusable read-only EF queries for dashboards and reports. These queries keep the
    /// filters and Includes that used to live in the generic service repositories.
    /// </summary>
    internal static class DashboardReportQueries
    {
        internal static IQueryable<Book> Books(FreeBookDbContext context) =>
            context.books.AsNoTracking();

        internal static IQueryable<Book> ActiveBooks(FreeBookDbContext context) =>
            Books(context).Where(book => book.CurrentState > 0);

        internal static IQueryable<Book> ActiveBooks(
            FreeBookDbContext context,
            Guid? authorId,
            Guid? categoryId) =>
            ActiveBooks(context)
                .Where(book => !authorId.HasValue || book.AuthorId == authorId.Value)
                .Where(book => !categoryId.HasValue || book.CategoryId == categoryId.Value);

        internal static IQueryable<BookCopy> BookCopies(FreeBookDbContext context) =>
            context.BookCopies.AsNoTracking();

        internal static IQueryable<BookCopy> ActiveBookCopies(
            FreeBookDbContext context,
            Guid? authorId,
            Guid? categoryId) =>
            from copy in BookCopies(context)
            join book in ActiveBooks(context, authorId, categoryId) on copy.BookId equals book.Id
            select copy;

        internal static IQueryable<RecentRentalRow> RecentRentals(FreeBookDbContext context, int take) =>
            context.Rentals.AsNoTracking()
                .OrderByDescending(rental => rental.StartDate)
                .Take(take)
                .Select(rental => new RecentRentalRow
                {
                    SubscriberName = rental.Subscriber != null
                        ? rental.Subscriber.FirstName + " " + rental.Subscriber.LastName
                        : null,
                    StartDate = rental.StartDate,
                    RentalCopyCount = rental.RentalCopies.Count()
                });

        internal static IQueryable<RecentSubscriberRow> RecentSubscribers(FreeBookDbContext context, int take) =>
            context.Subscribers.AsNoTracking()
                .OrderByDescending(subscriber => subscriber.CreatedOn)
                .ThenBy(subscriber => subscriber.FirstName)
                .ThenBy(subscriber => subscriber.LastName)
                .Take(take)
                .Select(subscriber => new RecentSubscriberRow
                {
                    FullName = subscriber.FirstName + " " + subscriber.LastName,
                    AreaName = subscriber.Area != null ? subscriber.Area.Name : null,
                    GovernorateName = subscriber.Governorate != null ? subscriber.Governorate.Name : null
                });

        internal static IQueryable<OverdueRentalRow> OverdueRentalRows(
            FreeBookDbContext context,
            DateTime now,
            int take) =>
            (from rental in context.Rentals.AsNoTracking()
             from copy in rental.RentalCopies
             where copy.ReturnDate == null && copy.EndDate < now
             orderby rental.StartDate descending
             select new OverdueRentalRow
             {
                 SubscriberName = rental.Subscriber != null
                     ? rental.Subscriber.FirstName + " " + rental.Subscriber.LastName
                     : null,
                 BookName = copy.BookCopy != null && copy.BookCopy.Book != null
                     ? copy.BookCopy.Book.Name
                     : null,
                 EndDate = copy.EndDate,
                 PenaltyPaid = rental.PenaltyPaid
             }).Take(take);

        internal static IQueryable<ExpiringSubscriptionRow> ExpiringSubscriptionRows(
            FreeBookDbContext context,
            DateTime now,
            DateTime warningTo,
            int take) =>
            context.Subscriptions.AsNoTracking()
                .Where(subscription => subscription.EndDate >= now && subscription.EndDate <= warningTo)
                .OrderByDescending(subscription => subscription.StartDate)
                .Take(take)
                .Select(subscription => new ExpiringSubscriptionRow
                {
                    SubscriberName = subscription.Subscriber != null
                        ? subscription.Subscriber.FirstName + " " + subscription.Subscriber.LastName
                        : null,
                    EndDate = subscription.EndDate
                });

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

        internal static IQueryable<ReportFilterOption> GovernorateOptions(FreeBookDbContext context) =>
            context.Governorates.AsNoTracking()
                .OrderBy(governorate => governorate.Name)
                .Select(governorate => new ReportFilterOption
                {
                    Id = governorate.Id,
                    Name = governorate.Name
                });

        internal static IQueryable<NamedCount> TopRentedBooks(FreeBookDbContext context) =>
            from rentalCopy in context.RentalCopies.AsNoTracking()
            join bookCopy in context.BookCopies.AsNoTracking() on rentalCopy.BookCopyId equals bookCopy.Id
            join book in Books(context) on bookCopy.BookId equals book.Id
            group book by new { book.Id, book.Name } into grouped
            select new NamedCount
            {
                Name = grouped.Key.Name,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> TopRenters(FreeBookDbContext context) =>
            from rental in context.Rentals.AsNoTracking()
            join subscriber in context.Subscribers.AsNoTracking() on rental.SubscriberId equals subscriber.Id
            group subscriber by new { subscriber.Id, subscriber.FirstName, subscriber.LastName } into grouped
            select new NamedCount
            {
                Name = grouped.Key.FirstName + " " + grouped.Key.LastName,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> BooksByCategory(
            FreeBookDbContext context,
            Guid? authorId,
            Guid? categoryId) =>
            from book in ActiveBooks(context, authorId, categoryId)
            group book by book.Category != null ? book.Category.Name : "—" into grouped
            select new NamedCount
            {
                Name = grouped.Key,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> BooksByAuthor(
            FreeBookDbContext context,
            Guid? authorId,
            Guid? categoryId) =>
            from book in ActiveBooks(context, authorId, categoryId)
            group book by book.Author != null ? book.Author.Name : "—" into grouped
            select new NamedCount
            {
                Name = grouped.Key,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> SubscribersByGovernorate(
            FreeBookDbContext context,
            Guid? governorateId) =>
            from subscriber in context.Subscribers.AsNoTracking()
                .Where(subscriber => !governorateId.HasValue || subscriber.GovernorateId == governorateId.Value)
            group subscriber by subscriber.Governorate != null ? subscriber.Governorate.Name : "—" into grouped
            select new NamedCount
            {
                Name = grouped.Key,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> TopSubscriberRenters(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            Guid? governorateId) =>
            from rental in context.Rentals.AsNoTracking()
            join subscriber in context.Subscribers.AsNoTracking() on rental.SubscriberId equals subscriber.Id
            where !governorateId.HasValue || subscriber.GovernorateId == governorateId.Value
            where rental.StartDate >= rangeFrom && rental.StartDate < toExclusive
            group subscriber by new { subscriber.Id, subscriber.FirstName, subscriber.LastName } into grouped
            select new NamedCount
            {
                Name = grouped.Key.FirstName + " " + grouped.Key.LastName,
                Count = grouped.Count()
            };

        internal static IQueryable<ReportQueryHelpers.MonthCount> RentalMonths(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            Guid? authorId,
            Guid? categoryId) =>
            from rental in context.Rentals.AsNoTracking()
            where rental.StartDate >= rangeFrom && rental.StartDate < toExclusive
            where !authorId.HasValue || rental.RentalCopies.Any(copy =>
                copy.BookCopy != null && copy.BookCopy.Book.AuthorId == authorId.Value)
            where !categoryId.HasValue || rental.RentalCopies.Any(copy =>
                copy.BookCopy != null && copy.BookCopy.Book.CategoryId == categoryId.Value)
            group rental by new { rental.StartDate.Year, rental.StartDate.Month } into grouped
            select new ReportQueryHelpers.MonthCount
            {
                Year = grouped.Key.Year,
                Month = grouped.Key.Month,
                Count = grouped.Count()
            };

        internal static IQueryable<ReportQueryHelpers.MonthCount> SubscriptionMonths(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            Guid? governorateId) =>
            from subscription in context.Subscriptions.AsNoTracking()
            where !governorateId.HasValue || subscription.Subscriber.GovernorateId == governorateId.Value
            where subscription.StartDate >= rangeFrom && subscription.StartDate < toExclusive
            group subscription by new { subscription.StartDate.Year, subscription.StartDate.Month } into grouped
            select new ReportQueryHelpers.MonthCount
            {
                Year = grouped.Key.Year,
                Month = grouped.Key.Month,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> TopRentedBooksForBooks(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            List<Guid> bookIds) =>
            from rentalCopy in context.RentalCopies.AsNoTracking()
            join rental in context.Rentals.AsNoTracking() on rentalCopy.RentalId equals rental.Id
            join bookCopy in context.BookCopies.AsNoTracking() on rentalCopy.BookCopyId equals bookCopy.Id
            join book in Books(context) on bookCopy.BookId equals book.Id
            where rental.StartDate >= rangeFrom && rental.StartDate < toExclusive
            where bookIds.Contains(book.Id)
            group book by new { book.Id, book.Name } into grouped
            select new NamedCount
            {
                Name = grouped.Key.Name,
                Count = grouped.Count()
            };

        internal static IQueryable<ReportQueryHelpers.MonthCount> RentalMonthsForBooks(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            List<Guid> bookIds) =>
            from rental in context.Rentals.AsNoTracking()
            where rental.StartDate >= rangeFrom && rental.StartDate < toExclusive
            where rental.RentalCopies.Any(copy => copy.BookCopy != null && bookIds.Contains(copy.BookCopy.BookId))
            group rental by new { rental.StartDate.Year, rental.StartDate.Month } into grouped
            select new ReportQueryHelpers.MonthCount
            {
                Year = grouped.Key.Year,
                Month = grouped.Key.Month,
                Count = grouped.Count()
            };

    }
}
