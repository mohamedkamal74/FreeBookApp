using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Domin.Entity.Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class SubscriptionsController : Controller
    {
        private readonly ISubscriptionRepository _serviceSubscription;
        private readonly IServiceRepository<SubscriberDto, SubscriberCommand> _serviceSubscriber;
        private readonly UserManager<ApplicationUser> _userManager;
        private IFlashMessageService Flash { get; }

        public SubscriptionsController(
            ISubscriptionRepository serviceSubscription,
            IServiceRepository<SubscriberDto, SubscriberCommand> serviceSubscriber,
            UserManager<ApplicationUser> userManager,
            IFlashMessageService flash)
        {
            _serviceSubscription = serviceSubscription;
            _serviceSubscriber = serviceSubscriber;
            _userManager = userManager;
            Flash = flash;
        }

        public async Task<IActionResult> Subscriptions(SubscriptionViewModel model, string? filter = null)
        {
            var subsResult = await _serviceSubscription.GetAllAsync();
            var subsResult2 = await _serviceSubscriber.GetAllAsync();
            var subscriptions = subsResult.IsSuccess ? subsResult.Value ?? new List<SubscriptionDto>() : new List<SubscriptionDto>();
            var now = DateTime.UtcNow;

            if (string.Equals(filter, "active", StringComparison.OrdinalIgnoreCase))
            {
                subscriptions = subscriptions
                    .Where(subscription => subscription.StartDate <= now && subscription.EndDate >= now)
                    .ToList();
            }
            else if (string.Equals(filter, "expiring", StringComparison.OrdinalIgnoreCase))
            {
                var warningTo = now.AddDays(Domin.Entity.Helper.ExpiringSubscriptionWindowDays);
                subscriptions = subscriptions
                    .Where(subscription => subscription.EndDate >= now && subscription.EndDate <= warningTo)
                    .ToList();
            }

            return View(new SubscriptionViewModel
            {
                Subscriptions = subscriptions,
                Subscribers = subsResult2.IsSuccess ? subsResult2.Value ?? new List<SubscriberDto>() : new List<SubscriberDto>(),
                NewSubscription = new NewSubscription()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubscription(Guid Id)
        {
            var result = await _serviceSubscription.DeleteAsync(Id);
            if (result.IsSuccess)
                return RedirectToAction(nameof(Subscriptions));
            return RedirectToAction(nameof(Subscriptions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SubscriptionViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
                return RedirectToAction(nameof(Subscriptions));

            if (model.NewSubscription.SubscriptionId == null || model.NewSubscription.SubscriptionId == Guid.Empty.ToString())
            {
                // create
                var command = new SubscriptionCommand
                {
                    SubscriberId = model.NewSubscription.SubscriberId,
                    StartDate = model.NewSubscription.StartDate,
                    EndDate = model.NewSubscription.EndDate,
                    CreatedById = userId
                };
                var saveResult = await _serviceSubscription.SaveAsync(command);
                if (saveResult.IsSuccess)
                    Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                else
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
            }
            else
            {
                // update
                var existingResult = await _serviceSubscription.FindByIdAsync(Guid.Parse(model.NewSubscription.SubscriptionId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    var command = new SubscriptionCommand
                    {
                        SubscriberId = model.NewSubscription.SubscriberId,
                        StartDate = model.NewSubscription.StartDate,
                        EndDate = model.NewSubscription.EndDate,
                        CreatedById = userId
                    };
                    var saveResult = await _serviceSubscription.SaveAsync(command, existingResult.Value.Id);
                    if (saveResult.IsSuccess)
                        Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                    else
                        Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                }
            }

            return RedirectToAction(nameof(Subscriptions));
        }
    }
}
