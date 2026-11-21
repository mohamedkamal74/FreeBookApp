using System;
using System.Threading.Tasks;
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
    public class BooksController : Controller
    {
        private readonly IServiceRepository<Book> _serviceBook;
        private readonly IServiceRepository<Author> _serviceAuthor;
        private readonly IServiceRepository<Category> _serviceCategory;
        private readonly IServiceRepository<SubCategory> _serviceSubCategory;

        public BooksController(
            IServiceRepository<Book> serviceBook,
            IServiceRepository<Author> serviceAuthor,
            IServiceRepository<Category> serviceCategory,
            IServiceRepository<SubCategory> serviceSubCategory)
        {
            _serviceBook = serviceBook;
            _serviceAuthor = serviceAuthor;
            _serviceCategory = serviceCategory;
            _serviceSubCategory = serviceSubCategory;
        }

        public async Task<IActionResult> Books()
        {
            var booksResult = await _serviceBook.GetAllAsync();
            var authorsResult = await _serviceAuthor.GetAllAsync();
            var categoriesResult = await _serviceCategory.GetAllAsync();
            var subCategoriesResult = await _serviceSubCategory.GetAllAsync();

            return View(new BookViewModel
            {
                Books = booksResult.IsSuccess ? booksResult.Value ?? new List<Book>() : new List<Book>(),
                Authors = authorsResult.IsSuccess ? authorsResult.Value ?? new List<Author>() : new List<Author>(),
                Categories = categoriesResult.IsSuccess ? categoriesResult.Value ?? new List<Category>() : new List<Category>(),
                SubCategories = subCategoriesResult.IsSuccess ? subCategoriesResult.Value ?? new List<SubCategory>() : new List<SubCategory>(),
                NewBook = new NewBook()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(Guid Id)
        {
            await _serviceBook.DeleteAsync(Id);
            return RedirectToAction(nameof(Books));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(BookViewModel model)
        {
            var input = model.NewBook;
            var isEdit = Guid.TryParse(input.BookId, out var bookId) && bookId != Guid.Empty;

            if ((!string.IsNullOrWhiteSpace(input.BookId) && !isEdit) ||
                string.IsNullOrWhiteSpace(input.Name) ||
                input.AuthorId is null || input.AuthorId == Guid.Empty ||
                !ModelState.IsValid)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                return RedirectToAction(nameof(Books));
            }

            var duplicateResult = await _serviceBook.FindByAsync(input.Name);
            var duplicate = duplicateResult.IsSuccess ? duplicateResult.Value : null;
            if (duplicate is not null && (!isEdit || duplicate.Id != bookId))
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDuplicateName);
                return RedirectToAction(nameof(Books));
            }

            Book book;
            if (isEdit)
            {
                var existingResult = await _serviceBook.FindByIdAsync(bookId);
                if (!existingResult.IsSuccess || existingResult.Value is null)
                {
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                    return RedirectToAction(nameof(Books));
                }

                book = existingResult.Value;
            }
            else
            {
                book = new Book { Id = Guid.NewGuid() };
            }

            ApplyInput(input, book);
            var saveResult = await _serviceBook.SaveAsync(book);

            if (saveResult.IsSuccess)
            {
                SessionMsg(
                    Helper.Success,
                    isEdit ? Resources.ResourceWeb.lbUpdate : Resources.ResourceWeb.lbSave,
                    isEdit ? Resources.ResourceWeb.lbMsgUpdatedOk : Resources.ResourceWeb.lbMsgSavedOk);
            }
            else
            {
                SessionMsg(
                    Helper.Error,
                    isEdit ? Resources.ResourceWeb.lbNotUpdate : Resources.ResourceWeb.lbNotSave,
                    isEdit ? Resources.ResourceWeb.lbMsgNotUpdatedOk : Resources.ResourceWeb.lbMsgNotSavedOk);
            }

            return RedirectToAction(nameof(Books));
        }

        private static void ApplyInput(NewBook input, Book book)
        {
            book.Name = input.Name.Trim();
            book.AuthorId = input.AuthorId!.Value;
            book.Publisher = Clean(input.Publisher);
            book.PublishingDate = input.PublishingDate;
            book.ImageUrl = Clean(input.ImageUrl);
            book.ImageThumbnailUrl = Clean(input.ImageThumbnailUrl);
            book.ImagePublicId = Clean(input.ImagePublicId);
            book.Hall = Clean(input.Hall);
            book.IsAvailableForRental = input.IsAvailableForRental;
            book.Description = Clean(input.Description);
            book.CategoryId = input.CategoryId;
            book.SubCategoryId = input.SubCategoryId;
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
