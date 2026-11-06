using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domin.Resources;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.ViewModel
{
    public class RolesViewModel
    {
        public List<IdentityRole> Roles { get; set; } = new();
        public NewRole NewRole { get; set; } = new();
    }
    public class NewRole
    {
        public string? RoleId { get; set; }
        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "RoleName")]
        public string RoleName { get; set; } = string.Empty;

    }
}
