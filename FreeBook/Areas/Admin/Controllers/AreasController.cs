using FreeBook.Resources;
using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Domin.Entity.Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class AreasController : Controller
    {
        private readonly IServiceRepository<AreaDto, AreaCommand> _serviceArea;
        private readonly IServiceRepository<GovernorateDto, GovernorateCommand> _serviceGovernorate;
        private readonly IStringLocalizer<ResourceWeb> _localizer;
        private IFlashMessageService Flash { get; }

        public AreasController(
            IServiceRepository<AreaDto, AreaCommand> serviceArea,
            IServiceRepository<GovernorateDto, GovernorateCommand> serviceGovernorate,
            IStringLocalizer<ResourceWeb> localizer,
            IFlashMessageService flash)
        {
            _serviceArea = serviceArea;
            _serviceGovernorate = serviceGovernorate;
            _localizer = localizer;
            Flash = flash;
        }

        public async Task<IActionResult> Areas(AreaViewModel model)
        {
            var areasResult = await _serviceArea.GetAllAsync();
            var govResult = await _serviceGovernorate.GetAllAsync();
            return View(new AreaViewModel
            {
                Areas = areasResult.IsSuccess ? areasResult.Value ?? new List<AreaDto>() : new List<AreaDto>(),
                Governorates = govResult.IsSuccess ? govResult.Value ?? new List<GovernorateDto>() : new List<GovernorateDto>(),
                NewArea = new NewArea()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteArea(Guid Id)
        {
            var result = await _serviceArea.DeleteAsync(Id);
            if (result.IsSuccess)
                return RedirectToAction(nameof(Areas));
            Flash.Error(Resources.ResourceWeb.lbNotSave, result.Error?.Message ?? _localizer["msgAreaHasSubscribers"]);
            return RedirectToAction(nameof(Areas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AreaViewModel model)
        {
            if (model.NewArea.AreaId == null || model.NewArea.AreaId == Guid.Empty.ToString())
            {
                // create
                var command = new AreaCommand
                {
                    Name = model.NewArea.Name,
                    GovernorateId = model.NewArea.GovernorateId
                };
                var saveResult = await _serviceArea.SaveAsync(command);
                if (saveResult.IsSuccess)
                    Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                else
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
            }
            else
            {
                // update
                var existingResult = await _serviceArea.FindByIdAsync(Guid.Parse(model.NewArea.AreaId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    var command = new AreaCommand
                    {
                        Name = model.NewArea.Name,
                        GovernorateId = model.NewArea.GovernorateId
                    };
                    var saveResult = await _serviceArea.SaveAsync(command, existingResult.Value.Id);
                    if (saveResult.IsSuccess)
                        Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                    else
                        Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                }
            }

            return RedirectToAction(nameof(Areas));
        }
    }
}
