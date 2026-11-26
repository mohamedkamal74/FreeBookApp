using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface IPublicHomeQueryService
    {
        Task<Result<List<AvailableBookCardDto>>> GetAvailableBooksAsync(int take = 12);
    }
}
