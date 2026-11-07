using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;

namespace Infrastructure.IRepository
{
    public interface IServiceRepositoryLog<T> where T : class
    {
        Task<Result<List<T>>> GetAllAsync();
        Task<Result<T?>> FindByIdAsync(Guid id);
        Task<Result<bool>> SaveAsync(Guid id, Guid userId);
        Task<Result<bool>> UpdateAsync(Guid id, Guid userId);
        Task<Result<bool>> DeleteAsync(Guid id, Guid userId);
        Task<Result<bool>> DeleteLogAsync(Guid id);
    }
}
