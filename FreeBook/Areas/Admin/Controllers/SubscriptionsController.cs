using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class SubscriptionsController : Controller
    {
        private readonly IServiceRepository<Subscription> _serviceSubscription;
        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubscriptionsController(
            IServiceRepository<Subscription> serviceSubscription,
            IServiceRepository<Subscriber> serviceSubscriber,
            UserManager<ApplicationUser> userManager)
        {
            _serviceSubscription = serviceSubscription;
            _serviceSubscriber = serviceSubscriber;
            _userManager = userManager;
        }

        public async Task<IActionResult> Subscriptions(SubscriptionViewModel model)
        {
            var subsResult = await _serviceSubscription.GetAllAsync();
            var subsResult2 = await _serviceSubscriber.GetAllAsync();
            return View(new SubscriptionViewModel
            {
                Subscriptions = subsResult.IsSuccess ? subsResult.Value ?? new List<Subscription>() : new List<Subscription>(),
                Subscribers = subsResult2.IsSuccess ? subsResult2.Value ?? new List<Subscriber>() : new List<Subscriber>(),
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
                var newSubscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    SubscriberId = model.NewSubscription.SubscriberId,
                    StartDate = model.NewSubscription.StartDate,
                    EndDate = model.NewSubscription.EndDate,
                    CreatedById = userId,
                    CreatedOn = DateTime.UtcNow
                };
                var saveResult = await _serviceSubscription.SaveAsync(newSubscription);
                if (saveResult.IsSuccess)
                    SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                else
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
            }
            else
            {
                // update
                var existingResult = await _serviceSubscription.FindByIdAsync(Guid.Parse(model.NewSubscription.SubscriptionId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    existingResult.Value.SubscriberId = model.NewSubscription.SubscriberId;
                    existingResult.Value.StartDate = model.NewSubscription.StartDate;
                    existingResult.Value.EndDate = model.NewSubscription.EndDate;
                    existingResult.Value.CreatedById = userId;
                    var saveResult = await _serviceSubscription.SaveAsync(existingResult.Value);
                    if (saveResult.IsSuccess)
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                }
            }

            return RedirectToAction(nameof(Subscriptions));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
