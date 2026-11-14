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
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            var booksResult = await _serviceBook.GetAllAsync();
            return View(new BookCopyViewModel
            {
                BookCopies = copiesResult.IsSuccess ? copiesResult.Value ?? new List<BookCopy>() : new List<BookCopy>(),
                Books = booksResult.IsSuccess ? booksResult.Value ?? new List<Book>() : new List<Book>(),
                NewBookCopy = new NewBookCopy()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBookCopy(Guid Id)
        {
            var result = await _serviceBookCopy.DeleteAsync(Id);
            if (result.IsSuccess)
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
                var saveResult = await _serviceBookCopy.SaveAsync(newBookCopy);
                if (saveResult.IsSuccess)
                    SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                else
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
            }
            else
            {
                // update
                var existingResult = await _serviceBookCopy.FindByIdAsync(Guid.Parse(model.NewBookCopy.BookCopyId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    existingResult.Value.BookId = model.NewBookCopy.BookId;
                    existingResult.Value.EditionNumber = model.NewBookCopy.EditionNumber;
                    existingResult.Value.SerialNumber = model.NewBookCopy.SerialNumber;
                    existingResult.Value.IsAvailableForRental = model.NewBookCopy.IsAvailableForRental;
                    var saveResult = await _serviceBookCopy.SaveAsync(existingResult.Value);
                    if (saveResult.IsSuccess)
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
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
