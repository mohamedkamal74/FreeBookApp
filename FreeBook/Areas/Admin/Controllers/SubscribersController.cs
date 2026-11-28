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
    public class SubscribersController : Controller
    {
        private readonly ISubscriberRepository _serviceSubscriber;
        private readonly IServiceRepository<AreaDto, AreaCommand> _serviceArea;
        private readonly IServiceRepository<GovernorateDto, GovernorateCommand> _serviceGovernorate;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ResourceWeb> _localizer;
        private IFlashMessageService Flash { get; }

        public SubscribersController(
            ISubscriberRepository serviceSubscriber,
            IServiceRepository<AreaDto, AreaCommand> serviceArea,
            IServiceRepository<GovernorateDto, GovernorateCommand> serviceGovernorate,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<ResourceWeb> localizer,
            IFlashMessageService flash)
        {
            _serviceSubscriber = serviceSubscriber;
            _serviceArea = serviceArea;
            _serviceGovernorate = serviceGovernorate;
            _userManager = userManager;
            _localizer = localizer;
            Flash = flash;
        }

        public async Task<IActionResult> Subscribers(SubscriberViewModel model, string? filter = null)
        {
            var subsResult = await _serviceSubscriber.GetFilteredAsync(filter);
            var areasResult = await _serviceArea.GetAllAsync();
            var govResult = await _serviceGovernorate.GetAllAsync();
            var users = await _userManager.GetUsersInRoleAsync("User");
            return View(new SubscriberViewModel
            {
                Subscribers = subsResult.IsSuccess ? subsResult.Value ?? new List<SubscriberDto>() : new List<SubscriberDto>(),
                Areas = areasResult.IsSuccess ? areasResult.Value ?? new List<AreaDto>() : new List<AreaDto>(),
                Governorates = govResult.IsSuccess ? govResult.Value ?? new List<GovernorateDto>() : new List<GovernorateDto>(),
                ApplicationUsers = users.Select(ToUserDto)
                    .OrderBy(u => u.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
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
                var subscriber = subscriberResult.Value;
                var command = ToCommand(subscriber);
                command.IsBlackListed = !subscriber.IsBlackListed;
                await _serviceSubscriber.SaveAsync(command, subscriber.Id);
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

            var selectedUserId = string.IsNullOrWhiteSpace(model.NewSubscriber.ApplicationUserId)
                ? null
                : model.NewSubscriber.ApplicationUserId;
            if (selectedUserId is not null)
            {
                var eligibleUsers = await _userManager.GetUsersInRoleAsync("User");
                if (!eligibleUsers.Any(u => u.Id == selectedUserId))
                {
                    Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgSubscriberUserInvalid"]);
                    return RedirectToAction(nameof(Subscribers));
                }
            }

            if (model.NewSubscriber.SubscriberId == null || model.NewSubscriber.SubscriberId == Guid.Empty.ToString())
            {
                // create
                var findResult = await _serviceSubscriber.FindByAsync(model.NewSubscriber.NationalId);
                if (findResult.IsSuccess && findResult.Value != null)
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDuplicateName);
                else
                {
                    var command = new SubscriberCommand
                    {
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
                        IsBlackListed = model.NewSubscriber.IsBlackListed,
                        ApplicationUserId = selectedUserId
                    };
                    var saveResult = await _serviceSubscriber.SaveAsync(command);
                    if (saveResult.IsSuccess)
                        Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                    else
                        Flash.Error(
                            Resources.ResourceWeb.lbNotSave,
                            saveResult.Error?.Code == "USER_ALREADY_LINKED"
                                ? _localizer["msgSubscriberUserAlreadyLinked"]
                                : Resources.ResourceWeb.lbMsgNotSavedOk);
                }
            }
            else
            {
                // update
                var existingResult = await _serviceSubscriber.FindByIdAsync(Guid.Parse(model.NewSubscriber.SubscriberId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    // Check for duplicate NationalId (excluding self)
                    var findResult = await _serviceSubscriber.FindByAsync(model.NewSubscriber.NationalId);
                    if (findResult.IsSuccess && findResult.Value != null && findResult.Value.Id != existingResult.Value.Id)
                    {
                        Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDuplicateName);
                    }
                    else
                    {
                        var command = new SubscriberCommand
                        {
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
                            IsBlackListed = model.NewSubscriber.IsBlackListed,
                            ApplicationUserId = selectedUserId
                        };
                        var saveResult = await _serviceSubscriber.SaveAsync(command, existingResult.Value.Id);
                        if (saveResult.IsSuccess)
                            Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                        else
                            Flash.Error(
                                Resources.ResourceWeb.lbNotUpdate,
                                saveResult.Error?.Code == "USER_ALREADY_LINKED"
                                    ? _localizer["msgSubscriberUserAlreadyLinked"]
                                    : Resources.ResourceWeb.lbMsgNotUpdatedOk);
                    }
                }
            }

            return RedirectToAction(nameof(Subscribers));
        }

        private static SubscriberCommand ToCommand(SubscriberDto subscriber) => new()
        {
            FirstName = subscriber.FirstName,
            LastName = subscriber.LastName,
            DateOfBirth = subscriber.DateOfBirth,
            NationalId = subscriber.NationalId,
            MobileNumber = subscriber.MobileNumber,
            HasWhatsApp = subscriber.HasWhatsApp,
            Email = subscriber.Email,
            ImageUrl = subscriber.ImageUrl,
            ImageThumbnailUrl = subscriber.ImageThumbnailUrl,
            AreaId = subscriber.AreaId,
            GovernorateId = subscriber.GovernorateId,
            Address = subscriber.Address,
            IsBlackListed = subscriber.IsBlackListed,
            ApplicationUserId = subscriber.ApplicationUserId
        };

        private static UserDto ToUserDto(ApplicationUser user) => new()
        {
            Id = user.Id,
            Name = string.IsNullOrWhiteSpace(user.Name) ? user.Email ?? user.UserName ?? user.Id : user.Name,
            Email = user.Email ?? user.UserName ?? string.Empty,
            ActiveUser = user.ActiveUser
        };
    }
}
