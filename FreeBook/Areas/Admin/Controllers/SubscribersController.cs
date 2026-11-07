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
            var subsResult = await _serviceSubscriber.GetAllAsync();
            var areasResult = await _serviceArea.GetAllAsync();
            var govResult = await _serviceGovernorate.GetAllAsync();
            return View(new SubscriberViewModel
            {
                Subscribers = subsResult.IsSuccess ? subsResult.Value ?? new List<Subscriber>() : new List<Subscriber>(),
                Areas = areasResult.IsSuccess ? areasResult.Value ?? new List<Area>() : new List<Area>(),
                Governorates = govResult.IsSuccess ? govResult.Value ?? new List<Governorate>() : new List<Governorate>(),
                NewSubscriber = new NewSubscriber()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubscriber(Guid Id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is not null)
            {
                var result = await _serviceSubscriber.DeleteAsync(Id);
                if (result.IsSuccess)
                    return RedirectToAction(nameof(Subscribers));
            }
            return RedirectToAction(nameof(Subscribers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlacklist(Guid Id)
        {
            var subscriberResult = await _serviceSubscriber.FindByIdAsync(Id);
            if (subscriberResult.IsSuccess && subscriberResult.Value is not null)
            {
                subscriberResult.Value.IsBlackListed = !subscriberResult.Value.IsBlackListed;
                await _serviceSubscriber.SaveAsync(subscriberResult.Value);
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
                var findResult = await _serviceSubscriber.FindByAsync(model.NewSubscriber.NationalId);
                if (findResult.IsSuccess && findResult.Value != null)
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
                    var saveResult = await _serviceSubscriber.SaveAsync(newSubscriber);
                    if (saveResult.IsSuccess)
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                }
            }
            else
            {
                // update
                var existingResult = await _serviceSubscriber.FindByIdAsync(Guid.Parse(model.NewSubscriber.SubscriberId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    existingResult.Value.FirstName = model.NewSubscriber.FirstName;
                    existingResult.Value.LastName = model.NewSubscriber.LastName;
                    existingResult.Value.DateOfBirth = model.NewSubscriber.DateOfBirth;
                    existingResult.Value.NationalId = model.NewSubscriber.NationalId;
                    existingResult.Value.MobileNumber = model.NewSubscriber.MobileNumber;
                    existingResult.Value.HasWhatsApp = model.NewSubscriber.HasWhatsApp;
                    existingResult.Value.Email = model.NewSubscriber.Email;
                    existingResult.Value.ImageUrl = model.NewSubscriber.ImageUrl;
                    existingResult.Value.ImageThumbnailUrl = model.NewSubscriber.ImageThumbnailUrl;
                    existingResult.Value.AreaId = model.NewSubscriber.AreaId;
                    existingResult.Value.GovernorateId = model.NewSubscriber.GovernorateId;
                    existingResult.Value.Address = model.NewSubscriber.Address;
                    existingResult.Value.IsBlackListed = model.NewSubscriber.IsBlackListed;
                    var saveResult = await _serviceSubscriber.SaveAsync(existingResult.Value);
                    if (saveResult.IsSuccess)
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
