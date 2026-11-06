using Domin.Entity;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class BookCopiesController : Controller
    {
        private readonly IServiceRepository<BookCopy> _serviceBookCopy;
        private readonly IServiceRepository<Book> _serviceBook;

        public BookCopiesController(
            IServiceRepository<BookCopy> serviceBookCopy,
            IServiceRepository<Book> serviceBook)
        {
            _serviceBookCopy = serviceBookCopy;
            _serviceBook = serviceBook;
        }

        public async Task<IActionResult> BookCopies(BookCopyViewModel model)
        {
            return View(new BookCopyViewModel
            {
                BookCopies = await _serviceBookCopy.GetAllAsync(),
                Books = await _serviceBook.GetAllAsync(),
                NewBookCopy = new NewBookCopy()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBookCopy(Guid Id)
        {
            if (await _serviceBookCopy.DeleteAsync(Id))
                return RedirectToAction(nameof(BookCopies));
            return RedirectToAction(nameof(BookCopies));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(BookCopyViewModel model)
        {
            if (model.NewBookCopy.BookCopyId == null || model.NewBookCopy.BookCopyId == Guid.Empty.ToString())
            {
                // create
                var newBookCopy = new BookCopy
                {
                    Id = Guid.NewGuid(),
                    BookId = model.NewBookCopy.BookId,
                    EditionNumber = model.NewBookCopy.EditionNumber,
                    SerialNumber = model.NewBookCopy.SerialNumber,
                    IsAvailableForRental = model.NewBookCopy.IsAvailableForRental
                };
                if (await _serviceBookCopy.SaveAsync(newBookCopy))
                    SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                else
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
            }
            else
            {
                // update
                var existingCopy = await _serviceBookCopy.FindByIdAsync(Guid.Parse(model.NewBookCopy.BookCopyId));
                if (existingCopy is not null)
                {
                    existingCopy.BookId = model.NewBookCopy.BookId;
                    existingCopy.EditionNumber = model.NewBookCopy.EditionNumber;
                    existingCopy.SerialNumber = model.NewBookCopy.SerialNumber;
                    existingCopy.IsAvailableForRental = model.NewBookCopy.IsAvailableForRental;
                    if (await _serviceBookCopy.SaveAsync(existingCopy))
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                }
            }

            return RedirectToAction(nameof(BookCopies));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
