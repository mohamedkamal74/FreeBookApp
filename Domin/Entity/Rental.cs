using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domin.Entity
{
    public class Rental
    {
        public Guid Id { get; set; }

        public Guid SubscriberId { get; set; }
        public Subscriber? Subscriber { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public bool PenaltyPaid { get; set; } = false;

        public decimal? TotalFine { get; set; }

        public ICollection<RentalCopy> RentalCopies { get; set; } = new List<RentalCopy>();
    }
}
