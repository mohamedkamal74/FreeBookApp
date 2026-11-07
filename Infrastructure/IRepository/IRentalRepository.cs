using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domin.Entity;

namespace Infrastructure.IRepository
{
    public interface IRentalRepository : IServiceRepository<Rental>
    {
        Task<List<Rental>> GetOverdueRentalsAsync();
        Task<Rental?> GetActiveRentalBySubscriberAsync(Guid subscriberId);
    }
}
