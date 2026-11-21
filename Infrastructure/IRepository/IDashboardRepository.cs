using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IDashboardRepository
    {
        Task<Result<GeneralDashboardData>> GetGeneralAsync(GeneralDashboardQuery query);
        Task<Result<SubscribersDashboardData>> GetSubscribersAsync(SubscribersDashboardQuery query);
        Task<Result<BooksDashboardData>> GetBooksAsync(BooksDashboardQuery query);
    }
}
