using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public sealed class ServicesPublicHome : IPublicHomeQueryService
    {
        private const int MaxTake = 24;

        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesPublicHome> _logger;

        public ServicesPublicHome(FreeBookDbContext context, ILogger<ServicesPublicHome> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<AvailableBookCardDto>>> GetAvailableBooksAsync(int take = 12)
        {
            try
            {
                var rows = await PublicHomeQueries.AvailableBooks(_context)
                    .Take(Math.Clamp(take, 1, MaxTake))
                    .ToListAsync();
                return Result.Success(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available books for the public home page");
                return Result.Failure<List<AvailableBookCardDto>>(
                    "PUBLIC_HOME_ERROR",
                    "Failed to load available books",
                    ex.Message);
            }
        }
    }
}
