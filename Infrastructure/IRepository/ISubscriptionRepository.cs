using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;

namespace Infrastructure.IRepository
{
    public interface ISubscriptionRepository : IServiceRepository<Subscription>
    {
        Task<Result<List<Subscription>>> GetExpiringSubscriptionsAsync(int days);
        Task<Result<List<Subscription>>> GetBySubscriberAsync(Guid subscriberId);
        Task<Result<List<Subscription>>> GetActiveSubscriptionsAsync();
    }
}
