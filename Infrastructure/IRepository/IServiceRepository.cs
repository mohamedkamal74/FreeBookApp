using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.IRepository
{
    public interface IServiceRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync();
        Task<T?> FindByIdAsync(Guid id);
        Task<T?> FindByAsync(string name);
        Task<bool> SaveAsync(T model);
        Task<bool> DeleteAsync(Guid id);
    }
}
