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
    public class AreaViewModel
    {
        public List<Area> Areas { get; set; } = new();
        public List<Governorate> Governorates { get; set; } = new();
        public NewArea NewArea { get; set; } = new();
    }

    public class NewArea
    {
        public string? AreaId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "AreaName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "GovernorateName")]
        public Guid GovernorateId { get; set; }
    }
}
