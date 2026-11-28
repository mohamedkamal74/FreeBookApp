using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IBookRepository : IServiceRepository<BookDto, BookCommand>
    {
        Task<Result<List<BookDto>>> GetFilteredAsync(Guid? authorId, Guid? categoryId, string? filter);
    }
}
