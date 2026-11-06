using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domin.Entity
{
    public class Book
    {
        public Guid Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "BookName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MinLength")]
        public string Name { get; set; } = null!;

        public Guid AuthorId { get; set; }
        public Author? Author { get; set; }

        [MaxLength(100)]
        public string? Publisher { get; set; }

        public DateTime? PublishingDate { get; set; }

        public string? ImageUrl { get; set; }

        public string? ImageThumbnailUrl { get; set; }

        public string? ImagePublicId { get; set; }

        [MaxLength(50)]
        public string? Hall { get; set; }

        public bool IsAvailableForRental { get; set; } = true;

        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public Guid? SubCategoryId { get; set; }
        [ForeignKey("SubCategoryId")]
        public SubCategory? SubCategory { get; set; }

        public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();

        public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();

        public int CurrentState { get; set; } = (int)Helper.ECurrentState.Active;
    }
}
