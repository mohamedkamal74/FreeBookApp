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
        private readonly IServiceRepository<SubscriberDto, SubscriberCommand> _serviceSubscriber;
        private readonly IServiceRepository<BookCopyDto, BookCopyCommand> _serviceBookCopy;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ResourceWeb> _localizer;
        private IFlashMessageService Flash { get; }

        public RentalsController(
            IRentalRepository serviceRental,
            IServiceRepository<SubscriberDto, SubscriberCommand> serviceSubscriber,
            IServiceRepository<BookCopyDto, BookCopyCommand> serviceBookCopy,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<ResourceWeb> localizer,
            IFlashMessageService flash)
        {
            _serviceRental = serviceRental;
            _serviceSubscriber = serviceSubscriber;
            _serviceBookCopy = serviceBookCopy;
            _userManager = userManager;
            _localizer = localizer;
            Flash = flash;
        }

        public async Task<IActionResult> Rentals(RentalViewModel model)
        {
            var rentalsResult = await _serviceRental.GetAllAsync();
            var subsResult = await _serviceSubscriber.GetAllAsync();
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            return View(new RentalViewModel
            {
                Rentals = rentalsResult.IsSuccess ? rentalsResult.Value ?? new List<RentalDto>() : new List<RentalDto>(),
                Subscribers = subsResult.IsSuccess ? subsResult.Value ?? new List<SubscriberDto>() : new List<SubscriberDto>(),
                AvailableCopies = copiesResult.IsSuccess ? copiesResult.Value ?? new List<BookCopyDto>() : new List<BookCopyDto>(),
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
            if (!subscriberResult.IsSuccess || subscriberResult.Value is null || subscriberResult.Value.IsBlackListed)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalSubscriberUnavailable"]);
                return RedirectToAction(nameof(Rentals));
            }

            var subscriber = subscriberResult.Value;

            // Check active subscription
            var now = DateTime.UtcNow;
            var hasActiveSubscription = subscriber.Subscriptions?.Any(s => s.StartDate <= now && s.EndDate >= now) ?? false;
            if (!hasActiveSubscription)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalNoActiveSubscription"]);
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
            var selectedCopies = availableCopies.Where(c => model.SelectedCopyIds.Contains(c.Id) && c.IsAvailableForRental).ToList();
            if (selectedCopies.Count != model.SelectedCopyIds.Length)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCopyUnavailable"]);
                return RedirectToAction(nameof(Rentals));
            }

            var command = new RentalCommand
            {
                SubscriberId = model.NewRental.SubscriberId,
                StartDate = DateTime.UtcNow,
                PenaltyPaid = false,
                RentalCopies = new List<RentalCopyCommand>()
            };

            foreach (var copy in selectedCopies)
            {
                command.RentalCopies.Add(new RentalCopyCommand
                {
                    BookCopyId = copy.Id,
                    RentalDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays((int)Domin.Entity.RentalsConfigurations.RentalDuration)
                });
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

            var maxExtensionDate = rentalCopy.RentalDate.AddDays((int)Domin.Entity.RentalsConfigurations.MaxRentalDuration);
            var newEndDate = model.NewEndDate > rentalCopy.EndDate ? model.NewEndDate : rentalCopy.EndDate.AddDays(7);

            if (newEndDate > maxExtensionDate)
                newEndDate = maxExtensionDate;

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
