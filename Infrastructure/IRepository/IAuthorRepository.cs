using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IAuthorRepository : IServiceRepository<AuthorDto, AuthorCommand>
    {
        Task<Result<List<AuthorDto>>> GetAllActiveAsync();
        Task<Result<AuthorDto?>> FindByNameAsync(string name);
    }
}
