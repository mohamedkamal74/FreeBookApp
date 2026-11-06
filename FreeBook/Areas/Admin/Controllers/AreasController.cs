using Domin.Entity;
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
            return View(new AreaViewModel
            {
                Areas = await _serviceArea.GetAllAsync(),
                Governorates = await _serviceGovernorate.GetAllAsync(),
                NewArea = new NewArea()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteArea(Guid Id)
        {
            if (await _serviceArea.DeleteAsync(Id))
                return RedirectToAction(nameof(Areas));
            SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Cannot delete area with associated subscribers");
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
                if (await _serviceArea.SaveAsync(newArea))
                    SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                else
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
            }
            else
            {
                // update
                var existingArea = await _serviceArea.FindByIdAsync(Guid.Parse(model.NewArea.AreaId));
                if (existingArea is not null)
                {
                    existingArea.Name = model.NewArea.Name;
                    existingArea.GovernorateId = model.NewArea.GovernorateId;
                    if (await _serviceArea.SaveAsync(existingArea))
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
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
