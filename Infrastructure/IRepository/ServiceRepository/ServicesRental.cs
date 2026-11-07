using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesRental : IRentalRepository
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesRental> _logger;

        public ServicesRental(FreeBookDbContext context, ILogger<ServicesRental> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var result = await FindByIdAsync(id);
                if (!result.IsSuccess || result.Value is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Rental with id {id} not found");

                _context.Rentals.Remove(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rental {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete rental", ex.Message);
            }
        }

        public async Task<Result<Rental?>> FindByAsync(string name)
        {
            // Not applicable for rentals, return null
            return Result.Success<Rental?>(null);
        }

        public async Task<Result<Rental?>> FindByIdAsync(Guid id)
        {
            try
            {
                var rental = await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding rental by id {Id}", id);
                return Result.Failure<Rental?>("FIND_ERROR", "Failed to find rental", ex.Message);
            }
        }

        public async Task<Result<List<Rental>>> GetAllAsync()
        {
            try
            {
                var rentals = await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
                return Result.Success(rentals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all rentals");
                return Result.Failure<List<Rental>>("GET_ALL_ERROR", "Failed to get rentals", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Rental model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing rental");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    model.StartDate = DateTime.UtcNow;
                    _context.Rentals.Add(model);
                }
                else
                {
                    result.Value.SubscriberId = model.SubscriberId;
                    result.Value.PenaltyPaid = model.PenaltyPaid;
                    result.Value.TotalFine = model.TotalFine;
                    _context.Rentals.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving rental {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save rental", ex.Message);
            }
        }

        public async Task<Result<Rental?>> GetActiveRentalBySubscriberAsync(Guid subscriberId)
        {
            try
            {
                var rental = await _context.Rentals
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                    .FirstOrDefaultAsync(x => x.SubscriberId == subscriberId &&
                        x.RentalCopies.Any(rc => rc.ReturnDate == null));
                return Result.Success(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active rental for subscriber {SubscriberId}", subscriberId);
                return Result.Failure<Rental?>("FIND_ERROR", "Failed to get active rental", ex.Message);
            }
        }

        public async Task<Result<List<Rental>>> GetOverdueRentalsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var rentals = await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .Where(x => x.RentalCopies.Any(rc => rc.ReturnDate == null && rc.EndDate < now))
                    .OrderBy(x => x.StartDate)
                    .ToListAsync();
                return Result.Success(rentals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue rentals");
                return Result.Failure<List<Rental>>("GET_OVERDUE_ERROR", "Failed to get overdue rentals", ex.Message);
            }
        }
    }
}
