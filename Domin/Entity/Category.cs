using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domin.Entity
{
    public class Category
    {
        public Guid Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "CtegoryName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MinLength")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public int Currentstste { get; set; } = (int)Helper.ECurrentState.Active;

        public ICollection<BookCategory> Books { get; set; } = new List<BookCategory>();

        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }
}
