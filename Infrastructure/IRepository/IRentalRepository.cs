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
        Task<Result<List<RentalDto>>> GetActiveRentalsAsync();
        Task<Result<List<RentalDto>>> GetFilteredAsync(string? filter);
        Task<Result<RentalDto?>> GetActiveRentalBySubscriberAsync(Guid subscriberId);
    }
}
