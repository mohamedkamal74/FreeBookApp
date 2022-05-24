using Domin.Entity;
using Domin.Resources;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ViewModel
{
    public class RegisterViewModel
    {
        public List<VwUser>? Users { get; set; }
        public NewRegister? NewRegister { get; set; }
        public List<IdentityRole>? Roles { get; set; }
        public ChangePasswordViewModel? changePassword { get; set; }
    }

    public class NewRegister
    {
        public string? Id { get; set; }
        [Required(ErrorMessageResourceType =typeof(ResourceData),ErrorMessageResourceName = "RegisterName")]
        [MaxLength(20,ErrorMessageResourceType =typeof(ResourceData),ErrorMessageResourceName = "MaxLength")]
        [MinLength(5,ErrorMessageResourceType =typeof(ResourceData),ErrorMessageResourceName = "MinLength")]
        public string? Name { get; set; }
        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "RegisterEmail")]
        [EmailAddress(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "RegisterEmailError")]

        public string? Email { get; set; }
        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "RoleName")]

        public string? RoleName { get; set; }
        public string? ImageUser { get; set; }
        
        public bool ActiveUser { get; set; }
        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "Password")]
        [MaxLength(20, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(5, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string? Password { get; set; }
        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "ConfirmPassword")]
        [Compare("Password",ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "ConfirmPasswordError")]

        public string? ConfirmPassword { get; set; }
    }
}
