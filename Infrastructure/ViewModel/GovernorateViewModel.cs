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
    public class GovernorateViewModel
    {
        public List<Governorate> Governorates { get; set; } = new();
        public NewGovernorate NewGovernorate { get; set; } = new();
    }

    public class NewGovernorate
    {
        public string? GovernorateId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "GovernorateName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string Name { get; set; } = string.Empty;
    }
}
