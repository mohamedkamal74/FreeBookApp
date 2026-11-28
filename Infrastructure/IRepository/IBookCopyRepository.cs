using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IBookCopyRepository : IServiceRepository<BookCopyDto, BookCopyCommand>
    {
        Task<Result<List<BookCopyDto>>> GetFilteredAsync(Guid? authorId, Guid? categoryId, string? filter);
    }
}
