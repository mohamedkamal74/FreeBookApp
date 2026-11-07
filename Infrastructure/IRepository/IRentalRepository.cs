using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;

namespace Infrastructure.IRepository
{
    public interface IRentalRepository : IServiceRepository<Rental>
    {
        Task<Result<List<Rental>>> GetOverdueRentalsAsync();
        Task<Result<Rental?>> GetActiveRentalBySubscriberAsync(Guid subscriberId);
    }
}
