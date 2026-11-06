using Domin.Entity;
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
            return View(new SubscriptionViewModel
            {
                Subscriptions = await _serviceSubscription.GetAllAsync(),
                Subscribers = await _serviceSubscriber.GetAllAsync(),
                NewSubscription = new NewSubscription()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubscription(Guid Id)
        {
            if (await _serviceSubscription.DeleteAsync(Id))
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
                if (await _serviceSubscription.SaveAsync(newSubscription))
                    SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                else
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
            }
            else
            {
                // update
                var existingSubscription = await _serviceSubscription.FindByIdAsync(Guid.Parse(model.NewSubscription.SubscriptionId));
                if (existingSubscription is not null)
                {
                    existingSubscription.SubscriberId = model.NewSubscription.SubscriberId;
                    existingSubscription.StartDate = model.NewSubscription.StartDate;
                    existingSubscription.EndDate = model.NewSubscription.EndDate;
                    existingSubscription.CreatedById = userId;
                    if (await _serviceSubscription.SaveAsync(existingSubscription))
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
