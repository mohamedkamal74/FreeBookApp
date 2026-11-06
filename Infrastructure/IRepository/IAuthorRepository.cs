using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domin.Entity;

namespace Infrastructure.IRepository
{
    public interface IAuthorRepository : IServiceRepository<Author>
    {
        Task<List<Author>> GetAllActiveAsync();
        Task<Author?> FindByNameAsync(string name);
    }
}
