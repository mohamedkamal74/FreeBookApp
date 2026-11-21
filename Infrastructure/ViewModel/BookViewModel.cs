using System;
using System.ComponentModel.DataAnnotations;
using Domin.Entity;
using Domin.Resources;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.ViewModel
{
    public class BookViewModel
    {
        public List<Book> Books { get; set; } = new();
        public List<Author> Authors { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<SubCategory> SubCategories { get; set; } = new();
        public NewBook NewBook { get; set; } = new();
    }

    public class NewBook
    {
        public string? BookId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "BookName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "AuthorName")]
        public Guid? AuthorId { get; set; }

        [MaxLength(100)]
        public string? Publisher { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PublishingDate { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? CoverImage { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(500)]
        public string? ImageThumbnailUrl { get; set; }

        [MaxLength(100)]
        public string? ImagePublicId { get; set; }

        [MaxLength(50)]
        public string? Hall { get; set; }

        public bool IsAvailableForRental { get; set; } = true;

        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid? SubCategoryId { get; set; }
    }
}
