using System;
using System.IO;
using System.Threading.Tasks;
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
    [Area(Domin.Entity.Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class BooksController : Controller
    {
        private readonly IBookRepository _serviceBook;
        private readonly IServiceRepository<AuthorDto, AuthorCommand> _serviceAuthor;
        private readonly IServiceRepository<CategoryDto, CategoryCommand> _serviceCategory;
        private readonly IServiceRepository<SubCategoryDto, SubCategoryCommand> _serviceSubCategory;
        private readonly IImageService _imageService;
        private readonly IStringLocalizer<ResourceWeb> _localizer;
        private IFlashMessageService Flash { get; }

        public BooksController(
            IBookRepository serviceBook,
            IServiceRepository<AuthorDto, AuthorCommand> serviceAuthor,
            IServiceRepository<CategoryDto, CategoryCommand> serviceCategory,
            IServiceRepository<SubCategoryDto, SubCategoryCommand> serviceSubCategory,
            IImageService imageService,
            IStringLocalizer<ResourceWeb> localizer,
            IFlashMessageService flash)
        {
            _serviceBook = serviceBook;
            _serviceAuthor = serviceAuthor;
            _serviceCategory = serviceCategory;
            _serviceSubCategory = serviceSubCategory;
            _imageService = imageService;
            _localizer = localizer;
            Flash = flash;
        }

        public async Task<IActionResult> Books(Guid? authorId = null, Guid? categoryId = null, string? filter = null)
        {
            var booksResult = await _serviceBook.GetFilteredAsync(authorId, categoryId, filter);
            var authorsResult = await _serviceAuthor.GetAllAsync();
            var categoriesResult = await _serviceCategory.GetAllAsync();
            var subCategoriesResult = await _serviceSubCategory.GetAllAsync();

            return View(new BookViewModel
            {
                Books = booksResult.IsSuccess ? booksResult.Value ?? new List<BookDto>() : new List<BookDto>(),
                Authors = authorsResult.IsSuccess ? authorsResult.Value ?? new List<AuthorDto>() : new List<AuthorDto>(),
                Categories = categoriesResult.IsSuccess ? categoriesResult.Value ?? new List<CategoryDto>() : new List<CategoryDto>(),
                SubCategories = subCategoriesResult.IsSuccess ? subCategoriesResult.Value ?? new List<SubCategoryDto>() : new List<SubCategoryDto>(),
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
                Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                return RedirectToAction(nameof(Books));
            }

            if (hasCover && !IsValidCover(cover!))
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgCoverImageInvalid"]);
                return RedirectToAction(nameof(Books));
            }

            var duplicateResult = await _serviceBook.FindByAsync(input.Name);
            var duplicate = duplicateResult.IsSuccess ? duplicateResult.Value : null;
            if (duplicate is not null && (!isEdit || duplicate.Id != bookId))
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDuplicateName);
                return RedirectToAction(nameof(Books));
            }

            var command = new BookCommand();
            string? oldPublicId = null;
            if (isEdit)
            {
                var existingResult = await _serviceBook.FindByIdAsync(bookId);
                if (!existingResult.IsSuccess || existingResult.Value is null)
                {
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                    return RedirectToAction(nameof(Books));
                }

                command.Id = existingResult.Value.Id;
                oldPublicId = Clean(existingResult.Value.ImagePublicId);
            }

            ImageUploadResult? uploadResult = null;
            if (hasCover)
            {
                uploadResult = await _imageService.UploadAsync(cover!, "books");
                if (!uploadResult.Success ||
                    string.IsNullOrWhiteSpace(uploadResult.Url) ||
                    string.IsNullOrWhiteSpace(uploadResult.PublicId))
                {
                    Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgCoverImageUploadFailed"]);
                    return RedirectToAction(nameof(Books));
                }
            }

            ApplyInput(input, command);
            if (uploadResult is not null)
            {
                command.ImageUrl = uploadResult.Url;
                command.ImageThumbnailUrl = Clean(uploadResult.ThumbnailUrl);
                command.ImagePublicId = uploadResult.PublicId;
            }

            var saveResult = await _serviceBook.SaveAsync(command, isEdit ? bookId : null);
            if (!saveResult.IsSuccess)
            {
                if (uploadResult is not null)
                {
                    await TryDeleteImageAsync(uploadResult.PublicId);
                }

                Flash.Error(
                    isEdit ? Resources.ResourceWeb.lbNotUpdate : Resources.ResourceWeb.lbNotSave,
                    isEdit ? Resources.ResourceWeb.lbMsgNotUpdatedOk : Resources.ResourceWeb.lbMsgNotSavedOk);
                return RedirectToAction(nameof(Books));
            }

            if (uploadResult is not null && oldPublicId is not null &&
                !string.Equals(oldPublicId, uploadResult.PublicId, StringComparison.OrdinalIgnoreCase))
            {
                await DeleteOldCoverIfUnusedAsync(oldPublicId, command.Id);
            }

            Flash.Success(
                isEdit ? Resources.ResourceWeb.lbUpdate : Resources.ResourceWeb.lbSave,
                isEdit ? Resources.ResourceWeb.lbMsgUpdatedOk : Resources.ResourceWeb.lbMsgSavedOk);

            return RedirectToAction(nameof(Books));
        }

        private static bool IsValidCover(IFormFile cover) =>
            cover.Length > 0 &&
            cover.Length <= Domin.Entity.Helper.MaxImageSizeBytes &&
            Domin.Entity.Helper.AllowedImageExtensions.Contains(Path.GetExtension(cover.FileName));

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

        private static void ApplyInput(NewBook input, BookCommand command)
        {
            command.Name = input.Name.Trim();
            command.AuthorId = input.AuthorId!.Value;
            command.Publisher = Clean(input.Publisher);
            command.PublishingDate = input.PublishingDate;
            command.ImageUrl = Clean(input.ImageUrl);
            command.ImageThumbnailUrl = Clean(input.ImageThumbnailUrl);
            command.ImagePublicId = Clean(input.ImagePublicId);
            command.Hall = Clean(input.Hall);
            command.IsAvailableForRental = input.IsAvailableForRental;
            command.Description = Clean(input.Description);
            command.CategoryId = input.CategoryId;
            command.SubCategoryId = input.SubCategoryId;
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
