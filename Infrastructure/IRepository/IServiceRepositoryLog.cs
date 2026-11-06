using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.IRepository
{
    public interface IServiceRepositoryLog<T> where T : class
    {
        Task<List<T>> GetAllAsync();
        Task<T?> FindByIdAsync(Guid id);
        Task<bool> SaveAsync(Guid id, Guid userId);
        Task<bool> UpdateAsync(Guid id, Guid userId);
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<bool> DeleteLogAsync(Guid id);
    }
}
