using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;

namespace Infrastructure.IRepository
{
    public interface IAuthorRepository : IServiceRepository<Author>
    {
        Task<Result<List<Author>>> GetAllActiveAsync();
        Task<Result<Author?>> FindByNameAsync(string name);
    }
}
