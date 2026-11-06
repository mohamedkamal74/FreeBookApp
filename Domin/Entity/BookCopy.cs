using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domin.Entity
{
    public class BookCopy
    {
        public Guid Id { get; set; }

        public Guid BookId { get; set; }
        public Book? Book { get; set; }

        public bool IsAvailableForRental { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "EditionNumber")]
        public int EditionNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "SerialNumber")]
        public int SerialNumber { get; set; }

        public ICollection<RentalCopy> Rentals { get; set; } = new List<RentalCopy>();
    }
}
