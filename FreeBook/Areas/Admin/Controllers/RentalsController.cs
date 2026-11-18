using Domin.Entity;
using FreeBook.Resources;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class RentalsController : Controller
    {
        private readonly IServiceRepository<Rental> _serviceRental;
        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly IServiceRepository<BookCopy> _serviceBookCopy;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ResourceWeb> _localizer;

        public RentalsController(
            IServiceRepository<Rental> serviceRental,
            IServiceRepository<Subscriber> serviceSubscriber,
            IServiceRepository<BookCopy> serviceBookCopy,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<ResourceWeb> localizer)
        {
            _serviceRental = serviceRental;
            _serviceSubscriber = serviceSubscriber;
            _serviceBookCopy = serviceBookCopy;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Rentals(RentalViewModel model)
        {
            var rentalsResult = await _serviceRental.GetAllAsync();
            var subsResult = await _serviceSubscriber.GetAllAsync();
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            return View(new RentalViewModel
            {
                Rentals = rentalsResult.IsSuccess ? rentalsResult.Value ?? new List<Rental>() : new List<Rental>(),
                Subscribers = subsResult.IsSuccess ? subsResult.Value ?? new List<Subscriber>() : new List<Subscriber>(),
                AvailableCopies = copiesResult.IsSuccess ? copiesResult.Value ?? new List<BookCopy>() : new List<BookCopy>(),
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
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalSelectSubscriber"]);
                return RedirectToAction(nameof(Rentals));
            }

            if (model.SelectedCopyIds == null || model.SelectedCopyIds.Length == 0)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalSelectCopy"]);
                return RedirectToAction(nameof(Rentals));
            }

            if (model.SelectedCopyIds.Length > (int)RentalsConfigurations.MaxAllowedCopies)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, string.Format(_localizer["msgRentalMaxCopies"], (int)RentalsConfigurations.MaxAllowedCopies));
                return RedirectToAction(nameof(Rentals));
            }

            var subscriberResult = await _serviceSubscriber.FindByIdAsync(model.NewRental.SubscriberId);
            if (!subscriberResult.IsSuccess || subscriberResult.Value is null || subscriberResult.Value.IsBlackListed)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalSubscriberUnavailable"]);
                return RedirectToAction(nameof(Rentals));
            }

            var subscriber = subscriberResult.Value;

            // Check active subscription
            var now = DateTime.UtcNow;
            var hasActiveSubscription = subscriber.Subscriptions?.Any(s => s.StartDate <= now && s.EndDate >= now) ?? false;
            if (!hasActiveSubscription)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalNoActiveSubscription"]);
                return RedirectToAction(nameof(Rentals));
            }

            // Check available copies
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            if (!copiesResult.IsSuccess)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCopiesLoadFailed"]);
                return RedirectToAction(nameof(Rentals));
            }

            var availableCopies = copiesResult.Value ?? new List<BookCopy>();
            var selectedCopies = availableCopies.Where(c => model.SelectedCopyIds.Contains(c.Id) && c.IsAvailableForRental).ToList();
            if (selectedCopies.Count != model.SelectedCopyIds.Length)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCopyUnavailable"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rental = new Rental
            {
                Id = Guid.NewGuid(),
                SubscriberId = model.NewRental.SubscriberId,
                StartDate = DateTime.UtcNow,
                PenaltyPaid = false,
                RentalCopies = new List<RentalCopy>()
            };

            foreach (var copy in selectedCopies)
            {
                rental.RentalCopies.Add(new RentalCopy
                {
                    RentalId = rental.Id,
                    BookCopyId = copy.Id,
                    RentalDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays((int)RentalsConfigurations.RentalDuration)
                });
                copy.IsAvailableForRental = false;
                await _serviceBookCopy.SaveAsync(copy);
            }

            var saveResult = await _serviceRental.SaveAsync(rental);
            if (saveResult.IsSuccess)
                SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, _localizer["msgRentalCreated"]);
            else
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCreateFailed"]);

            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(RentalViewModel model)
        {
            if (model.RentalId == Guid.Empty || model.RentalCopyId == Guid.Empty)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalInvalidCopy"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rentalResult = await _serviceRental.FindByIdAsync(model.RentalId);
            if (!rentalResult.IsSuccess || rentalResult.Value is null)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalNotFound"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rental = rentalResult.Value;

            var rentalCopy = rental.RentalCopies.FirstOrDefault(rc => rc.BookCopyId == model.RentalCopyId);
            if (rentalCopy is null || rentalCopy.ReturnDate != null)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCopyAlreadyReturned"]);
                return RedirectToAction(nameof(Rentals));
            }

            rentalCopy.ReturnDate = DateTime.UtcNow;
            var bookCopyResult = await _serviceBookCopy.FindByIdAsync(model.RentalCopyId);
            if (bookCopyResult.IsSuccess && bookCopyResult.Value is not null)
            {
                bookCopyResult.Value.IsAvailableForRental = true;
                await _serviceBookCopy.SaveAsync(bookCopyResult.Value);
            }

            await _serviceRental.SaveAsync(rental);
            SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, _localizer["msgRentalReturned"]);
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendRental(RentalViewModel model)
        {
            if (model.RentalId == Guid.Empty || model.RentalCopyId == Guid.Empty)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalInvalidCopy"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rentalResult = await _serviceRental.FindByIdAsync(model.RentalId);
            if (!rentalResult.IsSuccess || rentalResult.Value is null)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalNotFound"]);
                return RedirectToAction(nameof(Rentals));
            }

            var rental = rentalResult.Value;

            var rentalCopy = rental.RentalCopies.FirstOrDefault(rc => rc.BookCopyId == model.RentalCopyId);
            if (rentalCopy is null || rentalCopy.ReturnDate != null)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgRentalCopyAlreadyReturned"]);
                return RedirectToAction(nameof(Rentals));
            }

            var maxExtensionDate = rentalCopy.RentalDate.AddDays((int)RentalsConfigurations.MaxRentalDuration);
            var newEndDate = model.NewEndDate > rentalCopy.EndDate ? model.NewEndDate : rentalCopy.EndDate.AddDays(7);

            if (newEndDate > maxExtensionDate)
                newEndDate = maxExtensionDate;

            rentalCopy.EndDate = newEndDate;
            rentalCopy.ExtendedOn = DateTime.UtcNow;

            await _serviceRental.SaveAsync(rental);
            SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, _localizer["msgRentalExtended"]);
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayPenalty(Guid rentalId)
        {
            var rentalResult = await _serviceRental.FindByIdAsync(rentalId);
            if (rentalResult.IsSuccess && rentalResult.Value is not null)
            {
                rentalResult.Value.PenaltyPaid = true;
                await _serviceRental.SaveAsync(rentalResult.Value);
                SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, _localizer["msgRentalPenaltyPaid"]);
            }
            return RedirectToAction(nameof(Rentals));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
