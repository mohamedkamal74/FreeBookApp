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
    public class GovernoratesController : Controller
    {
        private readonly IServiceRepository<GovernorateDto, GovernorateCommand> _serviceGovernorate;
        private readonly IStringLocalizer<ResourceWeb> _localizer;
        private IFlashMessageService Flash { get; }

        public GovernoratesController(
            IServiceRepository<GovernorateDto, GovernorateCommand> serviceGovernorate,
            IStringLocalizer<ResourceWeb> localizer,
            IFlashMessageService flash)
        {
            _serviceGovernorate = serviceGovernorate;
            _localizer = localizer;
            Flash = flash;
        }

        public async Task<IActionResult> Governorates(GovernorateViewModel model)
        {
            var result = await _serviceGovernorate.GetAllAsync();
            return View(new GovernorateViewModel
            {
                Governorates = result.IsSuccess ? result.Value ?? new List<GovernorateDto>() : new List<GovernorateDto>(),
                NewGovernorate = new NewGovernorate()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGovernorate(Guid Id)
        {
            var result = await _serviceGovernorate.DeleteAsync(Id);
            if (result.IsSuccess)
                return RedirectToAction(nameof(Governorates));
            Flash.Error(Resources.ResourceWeb.lbNotSave, result.Error?.Message ?? _localizer["msgGovernorateInUse"]);
            return RedirectToAction(nameof(Governorates));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(GovernorateViewModel model)
        {
            if (model.NewGovernorate.GovernorateId == null || model.NewGovernorate.GovernorateId == Guid.Empty.ToString())
            {
                // create
                var findResult = await _serviceGovernorate.FindByAsync(model.NewGovernorate.Name);
                if (findResult.IsSuccess && findResult.Value != null)
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDuplicateName);
                else
                {
                    var command = new GovernorateCommand { Name = model.NewGovernorate.Name };
                    var saveResult = await _serviceGovernorate.SaveAsync(command);
                    if (saveResult.IsSuccess)
                        Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                    else
                        Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                }
            }
            else
            {
                // update
                var existingResult = await _serviceGovernorate.FindByIdAsync(Guid.Parse(model.NewGovernorate.GovernorateId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    // Check for duplicate name (excluding self)
                    var findResult = await _serviceGovernorate.FindByAsync(model.NewGovernorate.Name);
                    if (findResult.IsSuccess && findResult.Value != null && findResult.Value.Id != existingResult.Value.Id)
                    {
                        Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDuplicateName);
                    }
                    else
                    {
                        var command = new GovernorateCommand { Name = model.NewGovernorate.Name };
                        var saveResult = await _serviceGovernorate.SaveAsync(command, existingResult.Value.Id);
                        if (saveResult.IsSuccess)
                            Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                        else
                            Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                    }
                }
            }

            return RedirectToAction(nameof(Governorates));
        }
    }
}
