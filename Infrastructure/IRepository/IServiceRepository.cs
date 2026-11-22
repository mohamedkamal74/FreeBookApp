using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;

namespace Infrastructure.IRepository
{
    /// <summary>
    /// CRUD contract expressed entirely in DTOs. Persistence entities stay inside
    /// Infrastructure; callers never receive or submit a Domin.Entity type.
    /// </summary>
    public interface IServiceRepository<TDto, TCommand>
        where TDto : class
        where TCommand : class
    {
        Task<Result<List<TDto>>> GetAllAsync();
        Task<Result<TDto?>> FindByIdAsync(Guid id);
        Task<Result<TDto?>> FindByAsync(string name);
        Task<Result<bool>> SaveAsync(TCommand command, Guid? id = null);
        Task<Result<bool>> DeleteAsync(Guid id);
    }
}
