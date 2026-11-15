using Domin.Entity;

namespace FreeBook.Models
{
    /// <summary>Current dashboard filter selections plus the option lists for the filter form.</summary>
    public class DashboardFilterViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Guid? AuthorId { get; set; }
        public Guid? CategoryId { get; set; }
        public List<Author> Authors { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
