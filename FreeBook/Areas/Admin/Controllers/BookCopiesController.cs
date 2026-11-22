using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Domin.Entity.Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class BookCopiesController : Controller
    {
        private readonly IServiceRepository<BookCopyDto, BookCopyCommand> _serviceBookCopy;
        private readonly IServiceRepository<BookDto, BookCommand> _serviceBook;
        private IFlashMessageService Flash { get; }

        public BookCopiesController(
            IServiceRepository<BookCopyDto, BookCopyCommand> serviceBookCopy,
            IServiceRepository<BookDto, BookCommand> serviceBook,
            IFlashMessageService flash)
        {
            _serviceBookCopy = serviceBookCopy;
            _serviceBook = serviceBook;
            Flash = flash;
        }

        public async Task<IActionResult> BookCopies(BookCopyViewModel model)
        {
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            var booksResult = await _serviceBook.GetAllAsync();
            return View(new BookCopyViewModel
            {
                BookCopies = copiesResult.IsSuccess ? copiesResult.Value ?? new List<BookCopyDto>() : new List<BookCopyDto>(),
                Books = booksResult.IsSuccess ? booksResult.Value ?? new List<BookDto>() : new List<BookDto>(),
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
                var command = new BookCopyCommand
                {
                    BookId = model.NewBookCopy.BookId,
                    EditionNumber = model.NewBookCopy.EditionNumber,
                    SerialNumber = model.NewBookCopy.SerialNumber,
                    IsAvailableForRental = model.NewBookCopy.IsAvailableForRental
                };
                var saveResult = await _serviceBookCopy.SaveAsync(command);
                if (saveResult.IsSuccess)
                    Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                else
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
            }
            else
            {
                // update
                var existingResult = await _serviceBookCopy.FindByIdAsync(Guid.Parse(model.NewBookCopy.BookCopyId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    var command = new BookCopyCommand
                    {
                        Id = existingResult.Value.Id,
                        BookId = model.NewBookCopy.BookId,
                        EditionNumber = model.NewBookCopy.EditionNumber,
                        SerialNumber = model.NewBookCopy.SerialNumber,
                        IsAvailableForRental = model.NewBookCopy.IsAvailableForRental
                    };
                    var saveResult = await _serviceBookCopy.SaveAsync(command, existingResult.Value.Id);
                    if (saveResult.IsSuccess)
                        Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                    else
                        Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                }
            }

            return RedirectToAction(nameof(BookCopies));
        }
    }
}
