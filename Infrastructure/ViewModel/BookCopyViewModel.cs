using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domin.Entity;
using Domin.Resources;
using Infrastructure.ViewModel;

namespace Infrastructure.ViewModel
{
    public class BookCopyViewModel
    {
        public List<BookCopy> BookCopies { get; set; } = new();
        public List<Book> Books { get; set; } = new();
        public NewBookCopy NewBookCopy { get; set; } = new();
    }

    public class NewBookCopy
    {
        public string? BookCopyId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "BookName")]
        public Guid BookId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "EditionNumber")]
        [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "EditionNumber")]
        public int EditionNumber { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "SerialNumber")]
        [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "SerialNumber")]
        public int SerialNumber { get; set; }

        public bool IsAvailableForRental { get; set; } = true;
    }
}
