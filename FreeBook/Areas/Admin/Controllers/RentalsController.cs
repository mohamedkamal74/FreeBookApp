using FreeBook.Resources;
using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Domin.Entity.Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class RentalsController : Controller
    {
        private readonly IRentalRepository _serviceRental;
        private readonly IRentalRequestRepository _serviceRentalRequest;
        private readonly IServiceRepository<SubscriberDto, SubscriberCommand> _serviceSubscriber;
        private readonly IServiceRepository<BookCopyDto, BookCopyCommand> _serviceBookCopy;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ResourceWeb> _localizer;
        private IFlashMessageService Flash { get; }

        public RentalsController(
            IRentalRepository serviceRental,
            IRentalRequestRepository serviceRentalRequest,
            IServiceRepository<SubscriberDto, SubscriberCommand> serviceSubscriber,
            IServiceRepository<BookCopyDto, BookCopyCommand> serviceBookCopy,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<ResourceWeb> localizer,
            IFlashMessageService flash)
        {
            _serviceRental = serviceRental;
            _serviceRentalRequest = serviceRentalRequest;
            _serviceSubscriber = serviceSubscriber;
            _serviceBookCopy = serviceBookCopy;
            _userManager = userManager;
            _localizer = localizer;
            Flash = flash;
        }

        public async Task<IActionResult> Rentals(RentalViewModel model, string? filter = null)
        {
            var rentalsResult = await _serviceRental.GetFilteredAsync(filter);
            var subsResult = await _serviceSubscriber.GetAllAsync();
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            var pendingResult = await _serviceRentalRequest.GetPendingAsync();
            return View(new RentalViewModel
            {
                Rentals = rentalsResult.IsSuccess ? rentalsResult.Value ?? new List<RentalDto>() : new List<RentalDto>(),
                Subscribers = subsResult.IsSuccess ? subsResult.Value ?? new List<SubscriberDto>() : new List<SubscriberDto>(),
                AvailableCopies = copiesResult.IsSuccess ? copiesResult.Value ?? new List<BookCopyDto>() : new List<BookCopyDto>(),
                PendingRequests = pendingResult.IsSuccess ? pendingResult.Value ?? new List<RentalRequestDto>() : new List<RentalRequestDto>(),
                NewRental = new NewRental()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRental(Guid Id)
        {
            var result = await _serviceRental.DeleteAsync(Id);
            if (result.IsSuccess)
                return RedirectToAction(nameof(Rentals));
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRental(RentalViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
                return RedirectToAction(nameof(Rentals));

            if (model.NewRental.SubscriberId == Guid.Empty)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalSelectSubscriber"]);
                return RedirectToAction(nameof(Rentals));
            }

            if (model.SelectedCopyIds == null || model.SelectedCopyIds.Length == 0)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalSelectCopy"]);
                return RedirectToAction(nameof(Rentals));
            }

            if (model.SelectedCopyIds.Length > (int)Domin.Entity.RentalsConfigurations.MaxAllowedCopies)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, string.Format(_localizer["msgRentalMaxCopies"], (int)Domin.Entity.RentalsConfigurations.MaxAllowedCopies));
                return RedirectToAction(nameof(Rentals));
            }

            var subscriberResult = await _serviceSubscriber.FindByIdAsync(model.NewRental.SubscriberId);
            if (!subscriberResult.IsSuccess || subscriberResult.Value is null)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalSubscriberUnavailable"]);
                return RedirectToAction(nameof(Rentals));
            }

            // Check available copies
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            if (!copiesResult.IsSuccess)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCopiesLoadFailed"]);
                return RedirectToAction(nameof(Rentals));
            }

            var availableCopies = copiesResult.Value ?? new List<BookCopyDto>();
            var rule = RentalRules.BuildCommand(subscriberResult.Value, model.SelectedCopyIds, availableCopies);
            if (!rule.IsSuccess)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, LocalizeRentalRule(rule.Error));
                return RedirectToAction(nameof(Rentals));
            }

            var command = rule.Command!;
            var selectedIds = command.RentalCopies.Select(c => c.BookCopyId).ToHashSet();
            foreach (var copy in availableCopies.Where(c => selectedIds.Contains(c.Id)))
            {
                copy.IsAvailableForRental = false;
                await _serviceBookCopy.SaveAsync(ToCommand(copy), copy.Id);
            }

            var saveResult = await _serviceRental.SaveAsync(command);
            if (saveResult.IsSuccess)
                Flash.Success(Resources.ResourceWeb.lbSave, _localizer["msgRentalCreated"]);
            else
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCreateFailed"]);

            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(RentalViewModel model)
        {
            if (model.RentalId == Guid.Empty || model.RentalCopyId == Guid.Empty)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalInvalidCopy"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rentalResult = await _serviceRental.FindByIdAsync(model.RentalId);
            if (!rentalResult.IsSuccess || rentalResult.Value is null)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalNotFound"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rental = rentalResult.Value;

            var rentalCopy = rental.RentalCopies.FirstOrDefault(rc => rc.BookCopyId == model.RentalCopyId);
            if (rentalCopy is null || rentalCopy.ReturnDate != null)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCopyAlreadyReturned"]);
                return RedirectToAction(nameof(Rentals));
            }

            rentalCopy.ReturnDate = DateTime.UtcNow;
            var bookCopyResult = await _serviceBookCopy.FindByIdAsync(model.RentalCopyId);
            if (bookCopyResult.IsSuccess && bookCopyResult.Value is not null)
            {
                var copyCommand = ToCommand(bookCopyResult.Value);
                copyCommand.IsAvailableForRental = true;
                await _serviceBookCopy.SaveAsync(copyCommand, bookCopyResult.Value.Id);
            }

            await _serviceRental.SaveAsync(ToCommand(rental), rental.Id);
            Flash.Success(Resources.ResourceWeb.lbSave, _localizer["msgRentalReturned"]);
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendRental(RentalViewModel model)
        {
            if (model.RentalId == Guid.Empty || model.RentalCopyId == Guid.Empty)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalInvalidCopy"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rentalResult = await _serviceRental.FindByIdAsync(model.RentalId);
            if (!rentalResult.IsSuccess || rentalResult.Value is null)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalNotFound"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rental = rentalResult.Value;

            var rentalCopy = rental.RentalCopies.FirstOrDefault(rc => rc.BookCopyId == model.RentalCopyId);
            if (rentalCopy is null || rentalCopy.ReturnDate != null)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCopyAlreadyReturned"]);
                return RedirectToAction(nameof(Rentals));
            }

            var newEndDate = model.NewEndDate > rentalCopy.EndDate ? model.NewEndDate : rentalCopy.EndDate.AddDays(7);
            newEndDate = RentalRules.CapEndDate(rentalCopy.RentalDate, newEndDate);

            rentalCopy.EndDate = newEndDate;
            rentalCopy.ExtendedOn = DateTime.UtcNow;

            await _serviceRental.SaveAsync(ToCommand(rental), rental.Id);
            Flash.Success(Resources.ResourceWeb.lbSave, _localizer["msgRentalExtended"]);
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayPenalty(Guid rentalId)
        {
            var rentalResult = await _serviceRental.FindByIdAsync(rentalId);
            if (rentalResult.IsSuccess && rentalResult.Value is not null)
            {
                var command = ToCommand(rentalResult.Value);
                command.PenaltyPaid = true;
                await _serviceRental.SaveAsync(command, rentalResult.Value.Id);
                Flash.Success(Resources.ResourceWeb.lbSave, _localizer["msgRentalPenaltyPaid"]);
            }
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(RentalRequestViewModel model)
        {
            if (model.RequestId == Guid.Empty || model.SelectedCopyIds is null || model.SelectedCopyIds.Length == 0)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalRequestSelectCopy"]);
                return RedirectToAction(nameof(Rentals));
            }

            var result = await _serviceRentalRequest.ApproveAsync(
                model.RequestId,
                model.SelectedCopyIds,
                _userManager.GetUserId(User));
            if (result.IsSuccess)
                Flash.Success(Resources.ResourceWeb.lbSave, _localizer["msgRentalRequestApproved"]);
            else
                Flash.Error(Resources.ResourceWeb.lbNotSave, LocalizeRequestError(result.Error?.Code));

            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(RentalRequestViewModel model)
        {
            if (model.RequestId == Guid.Empty)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalRequestNotFound"]);
                return RedirectToAction(nameof(Rentals));
            }

            var result = await _serviceRentalRequest.RejectAsync(
                model.RequestId,
                _userManager.GetUserId(User),
                model.RejectionReason);
            if (result.IsSuccess)
                Flash.Success(Resources.ResourceWeb.lbUpdate, _localizer["msgRentalRequestRejected"]);
            else
                Flash.Error(Resources.ResourceWeb.lbNotUpdate, _localizer["msgRentalRequestRejectFailed"]);

            return RedirectToAction(nameof(Rentals));
        }

        private string LocalizeRentalRule(RentalRuleError error) => error switch
        {
            RentalRuleError.SubscriberNotFound or RentalRuleError.SubscriberBlacklisted =>
                _localizer["msgRentalSubscriberUnavailable"].Value,
            RentalRuleError.NoActiveSubscription => _localizer["msgRentalNoActiveSubscription"].Value,
            RentalRuleError.NoCopiesSelected => _localizer["msgRentalSelectCopy"].Value,
            RentalRuleError.TooManyCopies => string.Format(
                _localizer["msgRentalMaxCopies"].Value,
                (int)Domin.Entity.RentalsConfigurations.MaxAllowedCopies),
            RentalRuleError.CopyUnavailable => _localizer["msgRentalCopyUnavailable"].Value,
            RentalRuleError.DurationExceeded => string.Format(
                _localizer["msgRentalMaxDuration"].Value,
                (int)Domin.Entity.RentalsConfigurations.MaxRentalDuration),
            _ => _localizer["msgRentalCreateFailed"].Value
        };

        private string LocalizeRequestError(string? code)
        {
            if (code?.StartsWith("RULE_", StringComparison.Ordinal) == true &&
                Enum.TryParse<RentalRuleError>(code["RULE_".Length..], out var ruleError))
            {
                return LocalizeRentalRule(ruleError);
            }

            return code switch
            {
                "NOT_FOUND" or "INVALID_REQUEST" or "INVALID_STATUS" => _localizer["msgRentalRequestNotFound"].Value,
                "COPY_BOOK_MISMATCH" or "COPY_UNAVAILABLE" => _localizer["msgRentalCopyUnavailable"].Value,
                "DUPLICATE_PENDING" => _localizer["msgRentalRequestDuplicate"].Value,
                _ => _localizer["msgRentalRequestApproveFailed"].Value
            };
        }

        private static RentalCommand ToCommand(RentalDto rental)
        {
            var command = new RentalCommand
            {
                SubscriberId = rental.SubscriberId,
                StartDate = rental.StartDate,
                PenaltyPaid = rental.PenaltyPaid,
                TotalFine = rental.TotalFine
            };

            foreach (var copy in rental.RentalCopies)
            {
                command.RentalCopies.Add(new RentalCopyCommand
                {
                    BookCopyId = copy.BookCopyId,
                    RentalDate = copy.RentalDate,
                    EndDate = copy.EndDate,
                    ReturnDate = copy.ReturnDate,
                    ExtendedOn = copy.ExtendedOn
                });
            }

            return command;
        }

        private static BookCopyCommand ToCommand(BookCopyDto copy) => new()
        {
            Id = copy.Id,
            BookId = copy.BookId,
            IsAvailableForRental = copy.IsAvailableForRental,
            EditionNumber = copy.EditionNumber,
            SerialNumber = copy.SerialNumber
        };
    }
}
