using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class AreasController : Controller
    {
        private readonly IServiceRepository<Area> _serviceArea;
        private readonly IServiceRepository<Governorate> _serviceGovernorate;

        public AreasController(
            IServiceRepository<Area> serviceArea,
            IServiceRepository<Governorate> serviceGovernorate)
        {
            _serviceArea = serviceArea;
            _serviceGovernorate = serviceGovernorate;
        }

        public async Task<IActionResult> Areas(AreaViewModel model)
        {
            var areasResult = await _serviceArea.GetAllAsync();
            var govResult = await _serviceGovernorate.GetAllAsync();
            return View(new AreaViewModel
            {
                Areas = areasResult.IsSuccess ? areasResult.Value ?? new List<Area>() : new List<Area>(),
                Governorates = govResult.IsSuccess ? govResult.Value ?? new List<Governorate>() : new List<Governorate>(),
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
            SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, result.Error?.Message ?? "Cannot delete area with associated subscribers");
            return RedirectToAction(nameof(Areas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AreaViewModel model)
        {
            if (model.NewArea.AreaId == null || model.NewArea.AreaId == Guid.Empty.ToString())
            {
                // create
                var newArea = new Area
                {
                    Id = Guid.NewGuid(),
                    Name = model.NewArea.Name,
                    GovernorateId = model.NewArea.GovernorateId
                };
                var saveResult = await _serviceArea.SaveAsync(newArea);
                if (saveResult.IsSuccess)
                    SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                else
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
            }
            else
            {
                // update
                var existingResult = await _serviceArea.FindByIdAsync(Guid.Parse(model.NewArea.AreaId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    existingResult.Value.Name = model.NewArea.Name;
                    existingResult.Value.GovernorateId = model.NewArea.GovernorateId;
                    var saveResult = await _serviceArea.SaveAsync(existingResult.Value);
                    if (saveResult.IsSuccess)
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                }
            }

            return RedirectToAction(nameof(Areas));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
