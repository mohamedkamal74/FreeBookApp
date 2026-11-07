using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;

namespace Infrastructure.IRepository
{
    public interface IServiceRepository<T> where T : class
    {
        Task<Result<List<T>>> GetAllAsync();
        Task<Result<T?>> FindByIdAsync(Guid id);
        Task<Result<T?>> FindByAsync(string name);
        Task<Result<bool>> SaveAsync(T model);
        Task<Result<bool>> DeleteAsync(Guid id);
    }
}
