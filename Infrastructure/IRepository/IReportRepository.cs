using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IReportRepository
    {
        Task<Result<List<Rental>>> GetOverdueBooksAsync();
        Task<Result<List<PopularBookReport>>> GetPopularBooksAsync();
        Task<Result<List<Subscriber>>> GetActiveSubscribersAsync();
        Task<Result<ExpiringSubscriptionsData>> GetExpiringSubscriptionsAsync(int days);
        Task<Result<RentalHistoryData>> GetRentalHistoryAsync(Guid? subscriberId);
        Task<Result<List<BooksByCategoryReport>>> GetBooksByCategoryAsync();
        Task<Result<List<BooksByAuthorReport>>> GetBooksByAuthorAsync();
        Task<Result<BookInventoryReportData>> GetBooksAsync(BookInventoryQuery query);
    }
}
