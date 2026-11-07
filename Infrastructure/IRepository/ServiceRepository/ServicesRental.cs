using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesRental : IRentalRepository
    {
        private readonly FreeBookDbContext _context;

        public ServicesRental(FreeBookDbContext context)
        {
            _context = context;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var result = await FindByIdAsync(id);
                if (result is null)
                    return false;

                _context.Rentals.Remove(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Rental?> FindByAsync(string name)
        {
            // Not applicable for rentals, return null
            return null;
        }

        public async Task<Rental?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Rental>> GetAllAsync()
        {
            try
            {
                return await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Rental>();
            }
        }

        public async Task<bool> SaveAsync(Rental model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    model.StartDate = DateTime.UtcNow;
                    _context.Rentals.Add(model);
                }
                else
                {
                    result.SubscriberId = model.SubscriberId;
                    result.PenaltyPaid = model.PenaltyPaid;
                    result.TotalFine = model.TotalFine;
                    _context.Rentals.Update(result);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Rental?> GetActiveRentalBySubscriberAsync(Guid subscriberId)
        {
            try
            {
                return await _context.Rentals
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                    .FirstOrDefaultAsync(x => x.SubscriberId == subscriberId &&
                        x.RentalCopies.Any(rc => rc.ReturnDate == null));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Rental>> GetOverdueRentalsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                return await _context.Rentals
                    .Include(x => x.Subscriber)
                    .Include(x => x.RentalCopies)
                        .ThenInclude(rc => rc.BookCopy)
                            .ThenInclude(bc => bc.Book)
                    .Where(x => x.RentalCopies.Any(rc => rc.ReturnDate == null && rc.EndDate < now))
                    .OrderBy(x => x.StartDate)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Rental>();
            }
        }
    }
}
