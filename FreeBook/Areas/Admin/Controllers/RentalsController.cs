using Domin.Entity;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public RentalsController(
            IServiceRepository<Rental> serviceRental,
            IServiceRepository<Subscriber> serviceSubscriber,
            IServiceRepository<BookCopy> serviceBookCopy,
            UserManager<ApplicationUser> userManager)
        {
            _serviceRental = serviceRental;
            _serviceSubscriber = serviceSubscriber;
            _serviceBookCopy = serviceBookCopy;
            _userManager = userManager;
        }

        public async Task<IActionResult> Rentals(RentalViewModel model)
        {
            return View(new RentalViewModel
            {
                Rentals = await _serviceRental.GetAllAsync(),
                Subscribers = await _serviceSubscriber.GetAllAsync(),
                AvailableCopies = await _serviceBookCopy.GetAllAsync(),
                NewRental = new NewRental()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRental(Guid Id)
        {
            if (await _serviceRental.DeleteAsync(Id))
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
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Please select a subscriber");
                return RedirectToAction(nameof(Rentals));
            }

            if (model.SelectedCopyIds == null || model.SelectedCopyIds.Length == 0)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Please select at least one book copy");
                return RedirectToAction(nameof(Rentals));
            }

            if (model.SelectedCopyIds.Length > (int)RentalsConfigurations.MaxAllowedCopies)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, $"Maximum {(int)RentalsConfigurations.MaxAllowedCopies} copies allowed per rental");
                return RedirectToAction(nameof(Rentals));
            }

            var subscriber = await _serviceSubscriber.FindByIdAsync(model.NewRental.SubscriberId);
            if (subscriber is null || subscriber.IsBlackListed)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Subscriber not found or blacklisted");
                return RedirectToAction(nameof(Rentals));
            }

            // Check active subscription
            var now = DateTime.UtcNow;
            var hasActiveSubscription = subscriber.Subscriptions?.Any(s => s.StartDate <= now && s.EndDate >= now) ?? false;
            if (!hasActiveSubscription)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Subscriber does not have an active subscription");
                return RedirectToAction(nameof(Rentals));
            }

            // Check available copies
            var availableCopies = await _serviceBookCopy.GetAllAsync();
            var selectedCopies = availableCopies.Where(c => model.SelectedCopyIds.Contains(c.Id) && c.IsAvailableForRental).ToList();
            if (selectedCopies.Count != model.SelectedCopyIds.Length)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "One or more selected copies are not available");
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

            if (await _serviceRental.SaveAsync(rental))
                SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, "Rental created successfully");
            else
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Failed to create rental");

            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(RentalViewModel model)
        {
            if (model.RentalId == Guid.Empty || model.RentalCopyId == Guid.Empty)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Invalid rental or copy");
                return RedirectToAction(nameof(Rentals));
            }

            var rental = await _serviceRental.FindByIdAsync(model.RentalId);
            if (rental is null)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Rental not found");
                return RedirectToAction(nameof(Rentals));
            }

            var rentalCopy = rental.RentalCopies.FirstOrDefault(rc => rc.BookCopyId == model.RentalCopyId);
            if (rentalCopy is null || rentalCopy.ReturnDate != null)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Copy already returned or not found");
                return RedirectToAction(nameof(Rentals));
            }

            rentalCopy.ReturnDate = DateTime.UtcNow;
            var bookCopy = await _serviceBookCopy.FindByIdAsync(model.RentalCopyId);
            if (bookCopy is not null)
            {
                bookCopy.IsAvailableForRental = true;
                await _serviceBookCopy.SaveAsync(bookCopy);
            }

            await _serviceRental.SaveAsync(rental);
            SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, "Book returned successfully");
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendRental(RentalViewModel model)
        {
            if (model.RentalId == Guid.Empty || model.RentalCopyId == Guid.Empty)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Invalid rental or copy");
                return RedirectToAction(nameof(Rentals));
            }

            var rental = await _serviceRental.FindByIdAsync(model.RentalId);
            if (rental is null)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Rental not found");
                return RedirectToAction(nameof(Rentals));
            }

            var rentalCopy = rental.RentalCopies.FirstOrDefault(rc => rc.BookCopyId == model.RentalCopyId);
            if (rentalCopy is null || rentalCopy.ReturnDate != null)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Copy already returned or not found");
                return RedirectToAction(nameof(Rentals));
            }

            var maxExtensionDate = rentalCopy.RentalDate.AddDays((int)RentalsConfigurations.MaxRentalDuration);
            var newEndDate = model.NewEndDate > rentalCopy.EndDate ? model.NewEndDate : rentalCopy.EndDate.AddDays(7);

            if (newEndDate > maxExtensionDate)
                newEndDate = maxExtensionDate;

            rentalCopy.EndDate = newEndDate;
            rentalCopy.ExtendedOn = DateTime.UtcNow;

            await _serviceRental.SaveAsync(rental);
            SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, "Rental extended successfully");
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayPenalty(Guid rentalId)
        {
            var rental = await _serviceRental.FindByIdAsync(rentalId);
            if (rental is not null)
            {
                rental.PenaltyPaid = true;
                await _serviceRental.SaveAsync(rental);
                SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, "Penalty paid");
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
