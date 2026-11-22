using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface ISubscriptionRepository : IServiceRepository<SubscriptionDto, SubscriptionCommand>
    {
        Task<Result<List<SubscriptionDto>>> GetExpiringSubscriptionsAsync(int days);
        Task<Result<List<SubscriptionDto>>> GetBySubscriberAsync(Guid subscriberId);
        Task<Result<List<SubscriptionDto>>> GetActiveSubscriptionsAsync();
    }
}
