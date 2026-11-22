using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IRentalRepository : IServiceRepository<RentalDto, RentalCommand>
    {
        Task<Result<List<RentalDto>>> GetOverdueRentalsAsync();
        Task<Result<RentalDto?>> GetActiveRentalBySubscriberAsync(Guid subscriberId);
    }
}
