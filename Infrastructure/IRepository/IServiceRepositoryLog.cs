using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IServiceRepositoryLog<TDto> where TDto : class
    {
        Task<Result<List<TDto>>> GetAllAsync();
        Task<Result<TDto?>> FindByIdAsync(Guid id);
        Task<Result<bool>> SaveAsync(Guid id, Guid userId);
        Task<Result<bool>> UpdateAsync(Guid id, Guid userId);
        Task<Result<bool>> DeleteAsync(Guid id, Guid userId);
        Task<Result<bool>> DeleteLogAsync(Guid id);
    }
}
