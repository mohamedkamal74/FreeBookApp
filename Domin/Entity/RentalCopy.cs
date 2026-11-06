using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domin.Entity
{
    public class RentalCopy
    {
        public Guid RentalId { get; set; }
        public Rental? Rental { get; set; }

        public Guid BookCopyId { get; set; }
        public BookCopy? BookCopy { get; set; }

        public DateTime RentalDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public DateTime? ExtendedOn { get; set; }
    }
}
