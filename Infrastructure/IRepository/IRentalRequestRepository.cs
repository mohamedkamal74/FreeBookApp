using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IRentalRequestRepository : IServiceRepository<RentalRequestDto, RentalRequestCommand>
    {
        Task<Result<List<RentalRequestDto>>> GetPendingAsync();
        Task<Result<List<RentalRequestDto>>> GetByApplicationUserIdAsync(string applicationUserId);
        Task<Result<RentalRequestDto?>> FindByApplicationUserIdAsync(string applicationUserId);
        Task<Result<Guid>> CreateForUserAsync(string applicationUserId, Guid bookId, Guid? bookCopyId = null);
        Task<Result<Guid?>> ApproveAsync(Guid requestId, IReadOnlyCollection<Guid> selectedCopyIds, string? reviewedById);
        Task<Result<bool>> RejectAsync(Guid requestId, string? reviewedById, string? reason = null);
        Task<Result<bool>> CancelAsync(Guid requestId, string applicationUserId);
    }
}
