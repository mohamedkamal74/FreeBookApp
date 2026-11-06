using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.ViewModel
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }

        public string? ImageUser { get; set; }

        public bool ActiveUser { get; set; }

        public string? FullName { get; set; }

        public bool IsDeleted { get; set; }

        public string? CreatedById { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public string? LastUpdatedById { get; set; }

        public DateTime? LastUpdatedOn { get; set; }

        public Guid? SubscriberId { get; set; }
    }
}
