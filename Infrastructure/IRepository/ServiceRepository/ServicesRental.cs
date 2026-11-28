using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
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
                var rental = await _context.Rentals
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (rental is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Rental with id {id} not found");

                _context.Rentals.Remove(rental);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rental {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete rental", ex.Message);
            }
        }

        public async Task<Result<RentalDto?>> FindByAsync(string name)
        {
            // Not applicable for rentals, return null
            return await Task.FromResult(Result.Success<RentalDto?>(null));
        }

        public async Task<Result<RentalDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var rental = await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success<RentalDto?>(rental is null ? null : CrudMapping.ToDto(rental));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding rental by id {Id}", id);
                return Result.Failure<RentalDto?>("FIND_ERROR", "Failed to find rental", ex.Message);
            }
        }

        public async Task<Result<List<RentalDto>>> GetAllAsync()
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
                return Result.Success(rentals.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all rentals");
                return Result.Failure<List<RentalDto>>("GET_ALL_ERROR", "Failed to get rentals", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(RentalCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var rentalId = Guid.NewGuid();
                    var rental = new Rental
                    {
                        Id = rentalId,
                        SubscriberId = command.SubscriberId,
                        StartDate = DateTime.UtcNow,
                        PenaltyPaid = command.PenaltyPaid,
                        TotalFine = command.TotalFine,
                        RentalCopies = command.RentalCopies.Select(copy => new RentalCopy
                        {
                            RentalId = rentalId,
                            BookCopyId = copy.BookCopyId,
                            RentalDate = copy.RentalDate,
                            EndDate = copy.EndDate,
                            ReturnDate = copy.ReturnDate,
                            ExtendedOn = copy.ExtendedOn
                        }).ToList()
                    };

                    _context.Rentals.Add(rental);
                }
                else
                {
                    var rental = await _context.Rentals
                        .Include(x => x.RentalCopies)
                        .FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (rental is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Rental with id {id} not found");

                    rental.SubscriberId = command.SubscriberId;
                    rental.PenaltyPaid = command.PenaltyPaid;
                    rental.TotalFine = command.TotalFine;

                    foreach (var copy in command.RentalCopies)
                    {
                        var existing = rental.RentalCopies
                            .FirstOrDefault(rc => rc.BookCopyId == copy.BookCopyId);
                        if (existing is null)
                        {
                            var added = new RentalCopy
                            {
                                RentalId = rental.Id,
                                BookCopyId = copy.BookCopyId,
                                RentalDate = copy.RentalDate,
                                EndDate = copy.EndDate,
                                ReturnDate = copy.ReturnDate,
                                ExtendedOn = copy.ExtendedOn
                            };
                            rental.RentalCopies.Add(added);
                            _context.RentalCopies.Add(added);
                        }
                        else
                        {
                            existing.ReturnDate = copy.ReturnDate;
                            existing.EndDate = copy.EndDate;
                            existing.ExtendedOn = copy.ExtendedOn;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving rental {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save rental", ex.Message);
            }
        }

        public async Task<Result<List<RentalDto>>> GetActiveRentalsAsync()
        {
            try
            {
                var rentals = await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .Where(x => x.RentalCopies.Any(rc => rc.ReturnDate == null))
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
                return Result.Success(rentals.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active rentals");
                return Result.Failure<List<RentalDto>>("GET_ACTIVE_ERROR", "Failed to get active rentals", ex.Message);
            }
        }

        public async Task<Result<List<RentalDto>>> GetFilteredAsync(string? filter)
        {
            if (ListFilters.Matches(filter, ListFilters.Overdue))
            {
                return await GetOverdueRentalsAsync();
            }

            if (ListFilters.Matches(filter, ListFilters.Active))
            {
                return await GetActiveRentalsAsync();
            }

            return await GetAllAsync();
        }

        public async Task<Result<RentalDto?>> GetActiveRentalBySubscriberAsync(Guid subscriberId)
        {
            try
            {
                var rental = await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .FirstOrDefaultAsync(x => x.SubscriberId == subscriberId &&
                        x.RentalCopies.Any(rc => rc.ReturnDate == null));
                return Result.Success<RentalDto?>(rental is null ? null : CrudMapping.ToDto(rental));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active rental for subscriber {SubscriberId}", subscriberId);
                return Result.Failure<RentalDto?>("FIND_ERROR", "Failed to get active rental", ex.Message);
            }
        }

        public async Task<Result<List<RentalDto>>> GetOverdueRentalsAsync()
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
                return Result.Success(rentals.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue rentals");
                return Result.Failure<List<RentalDto>>("GET_OVERDUE_ERROR", "Failed to get overdue rentals", ex.Message);
            }
        }
    }
}
