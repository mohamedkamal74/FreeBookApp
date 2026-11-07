using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domin.Entity;

namespace Infrastructure.IRepository
{
    public interface ISubscriptionRepository : IServiceRepository<Subscription>
    {
        Task<List<Subscription>> GetExpiringSubscriptionsAsync(int days);
        Task<List<Subscription>> GetBySubscriberAsync(Guid subscriberId);
        Task<List<Subscription>> GetActiveSubscriptionsAsync();
    }
}
