using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    /// <summary>
    /// Lifecycle of member rental requests. Approval is the only path that
    /// creates a Rental and marks copies unavailable, in one transaction.
    /// </summary>
    public class ServicesRentalRequest : IRentalRequestRepository
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesRentalRequest> _logger;

        public ServicesRentalRequest(FreeBookDbContext context, ILogger<ServicesRentalRequest> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<RentalRequestDto>>> GetAllAsync()
        {
            try
            {
                var requests = await QueryWithDetails()
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();
                return Result.Success(requests.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rental requests");
                return Result.Failure<List<RentalRequestDto>>("GET_ALL_ERROR", "Failed to get rental requests", ex.Message);
            }
        }

        public async Task<Result<RentalRequestDto?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var request = await QueryWithDetails()
                    .FirstOrDefaultAsync(x =>
                        (x.Subscriber != null && x.Subscriber.FullName == trimmed) ||
                        (x.Book != null && x.Book.Name == trimmed));
                return Result.Success<RentalRequestDto?>(request is null ? null : CrudMapping.ToDto(request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding rental request {Name}", name);
                return Result.Failure<RentalRequestDto?>("FIND_ERROR", "Failed to find rental request", ex.Message);
            }
        }

        public async Task<Result<RentalRequestDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var request = await QueryWithDetails().FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success<RentalRequestDto?>(request is null ? null : CrudMapping.ToDto(request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding rental request {Id}", id);
                return Result.Failure<RentalRequestDto?>("FIND_ERROR", "Failed to find rental request", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(RentalRequestCommand command, Guid? id = null)
        {
            try
            {
                if (!await _context.Subscribers.AnyAsync(x => x.Id == command.SubscriberId))
                    return Result.Failure<bool>("SUBSCRIBER_NOT_FOUND", "Subscriber not found");
                if (!await _context.books.AnyAsync(x => x.Id == command.BookId))
                    return Result.Failure<bool>("BOOK_NOT_FOUND", "Book not found");
                if (command.BookCopyId.HasValue &&
                    !await _context.BookCopies.AnyAsync(x => x.Id == command.BookCopyId.Value && x.BookId == command.BookId))
                    return Result.Failure<bool>("COPY_NOT_FOUND", "Book copy not found for this book");

                if (id is null || id == Guid.Empty)
                {
                    var request = new RentalRequest
                    {
                        Id = Guid.NewGuid(),
                        SubscriberId = command.SubscriberId,
                        BookId = command.BookId,
                        BookCopyId = command.BookCopyId,
                        Status = CrudMapping.ToEntity(command.Status),
                        CreatedOn = DateTime.UtcNow,
                        ReviewedOn = command.ReviewedOn,
                        ReviewedById = command.ReviewedById,
                        RejectionReason = command.RejectionReason
                    };
                    _context.RentalRequests.Add(request);
                }
                else
                {
                    var request = await _context.RentalRequests.FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (request is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Rental request with id {id} not found");

                    request.SubscriberId = command.SubscriberId;
                    request.BookId = command.BookId;
                    request.BookCopyId = command.BookCopyId;
                    request.Status = CrudMapping.ToEntity(command.Status);
                    request.ReviewedOn = command.ReviewedOn;
                    request.ReviewedById = command.ReviewedById;
                    request.RejectionReason = command.RejectionReason;
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving rental request {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save rental request", ex.Message);
            }
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var request = await _context.RentalRequests.FirstOrDefaultAsync(x => x.Id == id);
                if (request is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Rental request with id {id} not found");

                _context.RentalRequests.Remove(request);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rental request {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete rental request", ex.Message);
            }
        }

        public async Task<Result<List<RentalRequestDto>>> GetPendingAsync()
        {
            try
            {
                var requests = await QueryWithDetails()
                    .Where(x => x.Status == RentalRequestStatus.Pending)
                    .OrderBy(x => x.CreatedOn)
                    .ToListAsync();
                return Result.Success(requests.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending rental requests");
                return Result.Failure<List<RentalRequestDto>>("GET_PENDING_ERROR", "Failed to get pending requests", ex.Message);
            }
        }

        public async Task<Result<RentalRequestDto?>> FindByApplicationUserIdAsync(string applicationUserId)
        {
            try
            {
                var request = await QueryWithDetails()
                    .Where(x => x.Subscriber != null && x.Subscriber.ApplicationUserId == applicationUserId)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefaultAsync();
                return Result.Success<RentalRequestDto?>(request is null ? null : CrudMapping.ToDto(request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding rental requests for user {UserId}", applicationUserId);
                return Result.Failure<RentalRequestDto?>("FIND_ERROR", "Failed to find rental request", ex.Message);
            }
        }

        public async Task<Result<Guid>> CreateForUserAsync(string applicationUserId, Guid bookId, Guid? bookCopyId = null)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
                return Result.Failure<Guid>("USER_REQUIRED", "A linked user is required");

            try
            {
                var subscriber = await _context.Subscribers
                    .Include(x => x.Subscriptions)
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);
                if (subscriber is null)
                    return Result.Failure<Guid>("SUBSCRIBER_NOT_LINKED", "User is not linked to a subscriber");

                var ruleError = RentalRules.ValidateSubscriber(CrudMapping.ToDto(subscriber));
                if (ruleError != RentalRuleError.None)
                    return Result.Failure<Guid>($"RULE_{ruleError}", ruleError.ToString());

                var book = await _context.books
                    .FirstOrDefaultAsync(x => x.Id == bookId && x.CurrentState == (int)Helper.ECurrentState.Active);
                if (book is null)
                    return Result.Failure<Guid>("BOOK_NOT_FOUND", "Book not found");

                if (bookCopyId.HasValue)
                {
                    var preferredCopy = await _context.BookCopies
                        .FirstOrDefaultAsync(x => x.Id == bookCopyId.Value && x.BookId == bookId);
                    if (preferredCopy is null || !preferredCopy.IsAvailableForRental)
                        return Result.Failure<Guid>("COPY_UNAVAILABLE", "Requested copy is not available");
                }

                var duplicate = await _context.RentalRequests.AnyAsync(x =>
                    x.SubscriberId == subscriber.Id && x.BookId == bookId && x.Status == RentalRequestStatus.Pending);
                if (duplicate)
                    return Result.Failure<Guid>("DUPLICATE_PENDING", "A pending request already exists for this book");

                var request = new RentalRequest
                {
                    Id = Guid.NewGuid(),
                    SubscriberId = subscriber.Id,
                    BookId = bookId,
                    BookCopyId = bookCopyId,
                    Status = RentalRequestStatus.Pending,
                    CreatedOn = DateTime.UtcNow
                };
                _context.RentalRequests.Add(request);
                await _context.SaveChangesAsync();
                return Result.Success(request.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rental request for user {UserId}", applicationUserId);
                return Result.Failure<Guid>("CREATE_ERROR", "Failed to create rental request", ex.Message);
            }
        }

        public async Task<Result<Guid?>> ApproveAsync(
            Guid requestId,
            IReadOnlyCollection<Guid> selectedCopyIds,
            string? reviewedById)
        {
            if (requestId == Guid.Empty)
                return Result.Failure<Guid?>("INVALID_REQUEST", "Invalid rental request");

            IDbContextTransaction? transaction = null;
            try
            {
                var request = await QueryWithDetails().FirstOrDefaultAsync(x => x.Id == requestId);
                if (request is null)
                    return Result.Failure<Guid?>("NOT_FOUND", $"Rental request with id {requestId} not found");
                if (request.Status != RentalRequestStatus.Pending)
                    return Result.Failure<Guid?>("INVALID_STATUS", "Rental request is not pending");
                if (request.Subscriber is null)
                    return Result.Failure<Guid?>("SUBSCRIBER_NOT_FOUND", "Subscriber not found");

                var ids = (selectedCopyIds ?? Array.Empty<Guid>()).Distinct().ToArray();
                var copies = await _context.BookCopies
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                var rule = RentalRules.BuildCommand(
                    CrudMapping.ToDto(request.Subscriber),
                    ids,
                    copies.Select(CrudMapping.ToDto).ToList());
                if (!rule.IsSuccess)
                    return Result.Failure<Guid?>($"RULE_{rule.Error}", rule.Error.ToString());
                if (copies.Any(x => x.BookId != request.BookId))
                    return Result.Failure<Guid?>("COPY_BOOK_MISMATCH", "Selected copy does not belong to the requested book");

                if (_context.Database.IsRelational())
                    transaction = await _context.Database.BeginTransactionAsync();

                var rental = CreateRentalEntity(rule.Command!);
                _context.Rentals.Add(rental);
                foreach (var copy in copies)
                    copy.IsAvailableForRental = false;

                request.Status = RentalRequestStatus.Approved;
                request.ReviewedOn = DateTime.UtcNow;
                request.ReviewedById = reviewedById;
                request.RejectionReason = null;

                await _context.SaveChangesAsync();
                if (transaction is not null)
                    await transaction.CommitAsync();
                return Result.Success<Guid?>(rental.Id);
            }
            catch (Exception ex)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving rental request {Id}", requestId);
                return Result.Failure<Guid?>("APPROVE_ERROR", "Failed to approve rental request", ex.Message);
            }
            finally
            {
                if (transaction is not null)
                    await transaction.DisposeAsync();
            }
        }

        public async Task<Result<bool>> RejectAsync(Guid requestId, string? reviewedById, string? reason = null)
        {
            try
            {
                var request = await _context.RentalRequests.FirstOrDefaultAsync(x => x.Id == requestId);
                if (request is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Rental request with id {requestId} not found");
                if (request.Status != RentalRequestStatus.Pending)
                    return Result.Failure<bool>("INVALID_STATUS", "Rental request is not pending");

                request.Status = RentalRequestStatus.Rejected;
                request.ReviewedOn = DateTime.UtcNow;
                request.ReviewedById = reviewedById;
                request.RejectionReason = reason;
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting rental request {Id}", requestId);
                return Result.Failure<bool>("REJECT_ERROR", "Failed to reject rental request", ex.Message);
            }
        }

        public async Task<Result<bool>> CancelAsync(Guid requestId, string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
                return Result.Failure<bool>("USER_REQUIRED", "A linked user is required");

            try
            {
                var request = await _context.RentalRequests
                    .Include(x => x.Subscriber)
                    .FirstOrDefaultAsync(x => x.Id == requestId);
                if (request is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Rental request with id {requestId} not found");
                if (request.Subscriber?.ApplicationUserId != applicationUserId)
                    return Result.Failure<bool>("FORBIDDEN", "Request belongs to another user");
                if (request.Status != RentalRequestStatus.Pending)
                    return Result.Failure<bool>("INVALID_STATUS", "Rental request is not pending");

                request.Status = RentalRequestStatus.Cancelled;
                request.ReviewedOn = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling rental request {Id}", requestId);
                return Result.Failure<bool>("CANCEL_ERROR", "Failed to cancel rental request", ex.Message);
            }
        }

        private IQueryable<RentalRequest> QueryWithDetails() => _context.RentalRequests
            .Include(x => x.Subscriber)
                .ThenInclude(s => s!.Subscriptions)
            .Include(x => x.Book)
            .Include(x => x.BookCopy);

        private static Rental CreateRentalEntity(RentalCommand command)
        {
            var rentalId = Guid.NewGuid();
            return new Rental
            {
                Id = rentalId,
                SubscriberId = command.SubscriberId,
                StartDate = command.StartDate,
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
        }
    }
}
