using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface ISubscriberRepository : IServiceRepository<SubscriberDto, SubscriberCommand>
    {
        Task<Result<List<SubscriberDto>>> GetBlacklistedSubscribersAsync();
        Task<Result<List<SubscriberDto>>> GetFilteredAsync(string? filter);
    }
}
