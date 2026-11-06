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
    public class SubscribersController : Controller
    {
        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly IServiceRepository<Area> _serviceArea;
        private readonly IServiceRepository<Governorate> _serviceGovernorate;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubscribersController(
            IServiceRepository<Subscriber> serviceSubscriber,
            IServiceRepository<Area> serviceArea,
            IServiceRepository<Governorate> serviceGovernorate,
            UserManager<ApplicationUser> userManager)
        {
            _serviceSubscriber = serviceSubscriber;
            _serviceArea = serviceArea;
            _serviceGovernorate = serviceGovernorate;
            _userManager = userManager;
        }

        public async Task<IActionResult> Subscribers(SubscriberViewModel model)
        {
            return View(new SubscriberViewModel
            {
                Subscribers = await _serviceSubscriber.GetAllAsync(),
                Areas = await _serviceArea.GetAllAsync(),
                Governorates = await _serviceGovernorate.GetAllAsync(),
                NewSubscriber = new NewSubscriber()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubscriber(Guid Id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is not null && await _serviceSubscriber.DeleteAsync(Id))
                return RedirectToAction(nameof(Subscribers));
            return RedirectToAction(nameof(Subscribers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlacklist(Guid Id)
        {
            var subscriber = await _serviceSubscriber.FindByIdAsync(Id);
            if (subscriber is not null)
            {
                subscriber.IsBlackListed = !subscriber.IsBlackListed;
                await _serviceSubscriber.SaveAsync(subscriber);
            }
            return RedirectToAction(nameof(Subscribers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SubscriberViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
                return RedirectToAction(nameof(Subscribers));

            if (model.NewSubscriber.SubscriberId == null || model.NewSubscriber.SubscriberId == Guid.Empty.ToString())
            {
                // create
                if (await _serviceSubscriber.FindByAsync(model.NewSubscriber.NationalId) != null)
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                else
                {
                    var newSubscriber = new Subscriber
                    {
                        Id = Guid.NewGuid(),
                        FirstName = model.NewSubscriber.FirstName,
                        LastName = model.NewSubscriber.LastName,
                        DateOfBirth = model.NewSubscriber.DateOfBirth,
                        NationalId = model.NewSubscriber.NationalId,
                        MobileNumber = model.NewSubscriber.MobileNumber,
                        HasWhatsApp = model.NewSubscriber.HasWhatsApp,
                        Email = model.NewSubscriber.Email,
                        ImageUrl = model.NewSubscriber.ImageUrl,
                        ImageThumbnailUrl = model.NewSubscriber.ImageThumbnailUrl,
                        AreaId = model.NewSubscriber.AreaId,
                        GovernorateId = model.NewSubscriber.GovernorateId,
                        Address = model.NewSubscriber.Address,
                        IsBlackListed = model.NewSubscriber.IsBlackListed
                    };
                    if (await _serviceSubscriber.SaveAsync(newSubscriber))
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                }
            }
            else
            {
                // update
                var existingSubscriber = await _serviceSubscriber.FindByIdAsync(Guid.Parse(model.NewSubscriber.SubscriberId));
                if (existingSubscriber is not null)
                {
                    existingSubscriber.FirstName = model.NewSubscriber.FirstName;
                    existingSubscriber.LastName = model.NewSubscriber.LastName;
                    existingSubscriber.DateOfBirth = model.NewSubscriber.DateOfBirth;
                    existingSubscriber.NationalId = model.NewSubscriber.NationalId;
                    existingSubscriber.MobileNumber = model.NewSubscriber.MobileNumber;
                    existingSubscriber.HasWhatsApp = model.NewSubscriber.HasWhatsApp;
                    existingSubscriber.Email = model.NewSubscriber.Email;
                    existingSubscriber.ImageUrl = model.NewSubscriber.ImageUrl;
                    existingSubscriber.ImageThumbnailUrl = model.NewSubscriber.ImageThumbnailUrl;
                    existingSubscriber.AreaId = model.NewSubscriber.AreaId;
                    existingSubscriber.GovernorateId = model.NewSubscriber.GovernorateId;
                    existingSubscriber.Address = model.NewSubscriber.Address;
                    existingSubscriber.IsBlackListed = model.NewSubscriber.IsBlackListed;
                    if (await _serviceSubscriber.SaveAsync(existingSubscriber))
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                }
            }

            return RedirectToAction(nameof(Subscribers));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
