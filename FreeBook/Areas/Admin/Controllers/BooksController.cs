using System;
using System.IO;
using System.Threading.Tasks;
using Domin.Entity;
using FreeBook.Resources;
using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

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
        private readonly IImageService _imageService;
        private readonly IStringLocalizer<ResourceWeb> _localizer;

        public BooksController(
            IServiceRepository<Book> serviceBook,
            IServiceRepository<Author> serviceAuthor,
            IServiceRepository<Category> serviceCategory,
            IServiceRepository<SubCategory> serviceSubCategory,
            IImageService imageService,
            IStringLocalizer<ResourceWeb> localizer)
        {
            _serviceBook = serviceBook;
            _serviceAuthor = serviceAuthor;
            _serviceCategory = serviceCategory;
            _serviceSubCategory = serviceSubCategory;
            _imageService = imageService;
            _localizer = localizer;
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
            var input = model.NewBook ?? new NewBook();
            var isEdit = Guid.TryParse(input.BookId, out var bookId) && bookId != Guid.Empty;
            var cover = input.CoverImage;
            var hasCover = cover is not null && cover.Length > 0;

            if ((!string.IsNullOrWhiteSpace(input.BookId) && !isEdit) ||
                string.IsNullOrWhiteSpace(input.Name) ||
                input.AuthorId is null || input.AuthorId == Guid.Empty ||
                !ModelState.IsValid)
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                return RedirectToAction(nameof(Books));
            }

            if (hasCover && !IsValidCover(cover!))
            {
                SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgCoverImageInvalid"]);
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
            string? oldPublicId = null;
            if (isEdit)
            {
                var existingResult = await _serviceBook.FindByIdAsync(bookId);
                if (!existingResult.IsSuccess || existingResult.Value is null)
                {
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                    return RedirectToAction(nameof(Books));
                }

                book = existingResult.Value;
                oldPublicId = Clean(book.ImagePublicId);
            }
            else
            {
                book = new Book { Id = Guid.NewGuid() };
            }

            ImageUploadResult? uploadResult = null;
            if (hasCover)
            {
                uploadResult = await _imageService.UploadAsync(cover!, "books");
                if (!uploadResult.Success ||
                    string.IsNullOrWhiteSpace(uploadResult.Url) ||
                    string.IsNullOrWhiteSpace(uploadResult.PublicId))
                {
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, _localizer["msgCoverImageUploadFailed"]);
                    return RedirectToAction(nameof(Books));
                }
            }

            ApplyInput(input, book);
            if (uploadResult is not null)
            {
                book.ImageUrl = uploadResult.Url;
                book.ImageThumbnailUrl = Clean(uploadResult.ThumbnailUrl);
                book.ImagePublicId = uploadResult.PublicId;
            }

            var saveResult = await _serviceBook.SaveAsync(book);
            if (!saveResult.IsSuccess)
            {
                if (uploadResult is not null)
                {
                    await TryDeleteImageAsync(uploadResult.PublicId);
                }

                SessionMsg(
                    Helper.Error,
                    isEdit ? Resources.ResourceWeb.lbNotUpdate : Resources.ResourceWeb.lbNotSave,
                    isEdit ? Resources.ResourceWeb.lbMsgNotUpdatedOk : Resources.ResourceWeb.lbMsgNotSavedOk);
                return RedirectToAction(nameof(Books));
            }

            if (uploadResult is not null && oldPublicId is not null &&
                !string.Equals(oldPublicId, uploadResult.PublicId, StringComparison.OrdinalIgnoreCase))
            {
                await DeleteOldCoverIfUnusedAsync(oldPublicId, book.Id);
            }

            SessionMsg(
                Helper.Success,
                isEdit ? Resources.ResourceWeb.lbUpdate : Resources.ResourceWeb.lbSave,
                isEdit ? Resources.ResourceWeb.lbMsgUpdatedOk : Resources.ResourceWeb.lbMsgSavedOk);

            return RedirectToAction(nameof(Books));
        }

        private static bool IsValidCover(IFormFile cover) =>
            cover.Length > 0 &&
            cover.Length <= Helper.MaxImageSizeBytes &&
            Helper.AllowedImageExtensions.Contains(Path.GetExtension(cover.FileName));

        private async Task DeleteOldCoverIfUnusedAsync(string oldPublicId, Guid currentBookId)
        {
            var allResult = await _serviceBook.GetAllAsync();
            if (!allResult.IsSuccess || allResult.Value is null)
            {
                return;
            }

            var stillUsed = allResult.Value.Any(x =>
                x.Id != currentBookId &&
                string.Equals(x.ImagePublicId, oldPublicId, StringComparison.OrdinalIgnoreCase));

            if (!stillUsed)
            {
                await TryDeleteImageAsync(oldPublicId);
            }
        }

        private async Task TryDeleteImageAsync(string publicId)
        {
            try
            {
                await _imageService.DeleteAsync(publicId);
            }
            catch
            {
                // Image cleanup must not turn a successful book save into a failure.
            }
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
