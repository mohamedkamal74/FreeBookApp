using System.ComponentModel.DataAnnotations;
using Domin.Resources;

namespace Infrastructure.ViewModel
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "CurrentPassword")]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "Password")]
        [MinLength(8, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string? NewPassword { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "ConfirmPassword")]
        [Compare("NewPassword", ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "ConfirmPasswordError")]
        public string? ConfirmPassword { get; set; }
    }
}
