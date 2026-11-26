using System.ComponentModel.DataAnnotations;
using Domin.Resources;

namespace Infrastructure.ViewModel
{
    /// <summary>Request form for sending a password reset link.</summary>
    public sealed class ForgotPasswordViewModel
    {
        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "RegisterEmail")]
        [EmailAddress(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "RegisterEmailError")]
        public string? Email { get; set; }
    }

    /// <summary>Reset form bound to a one-time Identity password reset token.</summary>
    public sealed class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "Password")]
        [MinLength(8, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        [MaxLength(20, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "ConfirmPassword")]
        [Compare(nameof(NewPassword), ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "ConfirmPasswordError")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
