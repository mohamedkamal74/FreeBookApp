using System.Globalization;
using FreeBook.Resources;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FreeBook.Controllers
{
    /// <summary>
    /// Self-service ordering and rental management. Every mutation is scoped to
    /// the signed-in Identity user; repository methods enforce the membership link.
    /// </summary>
    [Authorize(Roles = "User")]
    public sealed class MemberController : Controller
    {
        private readonly IPublicHomeQueryService _publicHomeQueries;
        private readonly IRentalRequestRepository _rentalRequests;
        private readonly IRentalRepository _rentals;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ResourceWeb> _localizer;
        private IFlashMessageService Flash { get; }

        public MemberController(
            IPublicHomeQueryService publicHomeQueries,
            IRentalRequestRepository rentalRequests,
            IRentalRepository rentals,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<ResourceWeb> localizer,
            IFlashMessageService flash)
        {
            _publicHomeQueries = publicHomeQueries;
            _rentalRequests = rentalRequests;
            _rentals = rentals;
            _userManager = userManager;
            _localizer = localizer;
            Flash = flash;
        }

        public async Task<IActionResult> Orders()
        {
            var userId = CurrentUserId();
            if (userId is null)
                return RedirectToAction("Login", "Accounts", new { area = Domin.Entity.Helper.Admin });

            var booksResult = await _publicHomeQueries.GetAvailableBooksAsync(24);
            var requestsResult = await _rentalRequests.GetByApplicationUserIdAsync(userId);

            return View(new MemberOrdersViewModel
            {
                AvailableBooks = booksResult.IsSuccess
                    ? booksResult.Value ?? new List<AvailableBookCardDto>()
                    : new List<AvailableBookCardDto>(),
                Requests = requestsResult.IsSuccess
                    ? requestsResult.Value ?? new List<RentalRequestDto>()
                    : new List<RentalRequestDto>()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestBook(MemberOrderCommand command)
        {
            var userId = CurrentUserId();
            if (userId is null)
                return RedirectToAction("Login", "Accounts", new { area = Domin.Entity.Helper.Admin });

            if (command.BookId == Guid.Empty)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgOrderBookNotFound"]);
                return RedirectToAction(nameof(Orders));
            }

            var result = await _rentalRequests.CreateForUserAsync(userId, command.BookId, command.BookCopyId);
            if (result.IsSuccess)
                Flash.Success(Resources.ResourceWeb.lbSave, _localizer["msgOrderCreated"]);
            else
                Flash.Error(Resources.ResourceWeb.lbNotSave, LocalizeRequestError(result.Error?.Code));

            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(MemberRequestAction command)
        {
            var userId = CurrentUserId();
            if (userId is null)
                return RedirectToAction("Login", "Accounts", new { area = Domin.Entity.Helper.Admin });

            if (command.RequestId == Guid.Empty)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgOrderRequestNotFound"]);
                return RedirectToAction(nameof(Orders));
            }

            var result = await _rentalRequests.CancelAsync(command.RequestId, userId);
            if (result.IsSuccess)
                Flash.Success(Resources.ResourceWeb.lbUpdate, _localizer["msgOrderCancelled"]);
            else
                Flash.Error(Resources.ResourceWeb.lbNotUpdate, LocalizeRequestError(result.Error?.Code));

            return RedirectToAction(nameof(Orders));
        }

        public async Task<IActionResult> MyRentals()
        {
            var userId = CurrentUserId();
            if (userId is null)
                return RedirectToAction("Login", "Accounts", new { area = Domin.Entity.Helper.Admin });

            var result = await _rentals.GetByApplicationUserIdAsync(userId);
            return View(new MemberRentalsViewModel
            {
                Rentals = result.IsSuccess ? result.Value ?? new List<RentalDto>() : new List<RentalDto>()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnCopy(MemberRentalAction command)
        {
            var userId = CurrentUserId();
            if (userId is null)
                return RedirectToAction("Login", "Accounts", new { area = Domin.Entity.Helper.Admin });

            var result = await _rentals.ReturnCopyForUserAsync(command.RentalId, command.BookCopyId, userId);
            if (result.IsSuccess)
                Flash.Success(Resources.ResourceWeb.lbSave, _localizer["msgRentalReturned"]);
            else
                Flash.Error(Resources.ResourceWeb.lbNotSave, LocalizeRentalActionError(result.Error?.Code));

            return RedirectToAction(nameof(MyRentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendCopy(MemberRentalAction command)
        {
            var userId = CurrentUserId();
            if (userId is null)
                return RedirectToAction("Login", "Accounts", new { area = Domin.Entity.Helper.Admin });

            DateTime? requestedEndDate = null;
            if (!string.IsNullOrWhiteSpace(command.NewEndDate))
            {
                if (!DateTime.TryParseExact(
                        command.NewEndDate,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedDate))
                {
                    Flash.Error(Resources.ResourceWeb.lbNotUpdate, _localizer["msgOrderInvalidDate"]);
                    return RedirectToAction(nameof(MyRentals));
                }

                requestedEndDate = DateTime.SpecifyKind(
                    parsedDate.Date.AddDays(1).AddTicks(-1),
                    DateTimeKind.Utc);
            }

            var result = await _rentals.ExtendCopyForUserAsync(
                command.RentalId,
                command.BookCopyId,
                userId,
                requestedEndDate);
            if (result.IsSuccess)
                Flash.Success(Resources.ResourceWeb.lbUpdate, _localizer["msgRentalExtended"]);
            else
                Flash.Error(Resources.ResourceWeb.lbNotUpdate, LocalizeRentalActionError(result.Error?.Code));

            return RedirectToAction(nameof(MyRentals));
        }

        private string? CurrentUserId() => _userManager.GetUserId(User);

        private string LocalizeRequestError(string? code)
        {
            if (code?.StartsWith("RULE_", StringComparison.Ordinal) == true)
            {
                return code switch
                {
                    "RULE_SubscriberNotFound" or "RULE_SubscriberBlacklisted" =>
                        _localizer["msgRentalSubscriberUnavailable"].Value,
                    "RULE_NoActiveSubscription" => _localizer["msgRentalNoActiveSubscription"].Value,
                    "RULE_CopyUnavailable" => _localizer["msgRentalCopyUnavailable"].Value,
                    _ => _localizer["msgOrderFailed"].Value
                };
            }

            return code switch
            {
                "USER_REQUIRED" or "SUBSCRIBER_NOT_LINKED" => _localizer["msgOrderNotLinked"].Value,
                "BOOK_NOT_FOUND" => _localizer["msgOrderBookNotFound"].Value,
                "COPY_UNAVAILABLE" => _localizer["msgRentalCopyUnavailable"].Value,
                "DUPLICATE_PENDING" => _localizer["msgRentalRequestDuplicate"].Value,
                "FORBIDDEN" or "NOT_FOUND" or "INVALID_REQUEST" =>
                    _localizer["msgOrderRequestNotFound"].Value,
                "INVALID_STATUS" => _localizer["msgOrderRequestNotPending"].Value,
                _ => _localizer["msgOrderFailed"].Value
            };
        }

        private string LocalizeRentalActionError(string? code) => code switch
        {
            "USER_REQUIRED" => _localizer["msgOrderNotLinked"].Value,
            "NOT_FOUND" or "FORBIDDEN" => _localizer["msgRentalNotFound"].Value,
            "COPY_NOT_FOUND" or "ALREADY_RETURNED" => _localizer["msgRentalCopyAlreadyReturned"].Value,
            "MAX_DURATION_REACHED" => _localizer["msgRentalMaxDurationReached"].Value,
            _ => _localizer["msgOrderFailed"].Value
        };
    }
}
