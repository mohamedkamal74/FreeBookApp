﻿using Domin.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ViewModel
{
    public class ChangePasswordViewModel
    {
        public string? Id { get; set; }
        [Required(ErrorMessageResourceType =typeof(ResourceData),ErrorMessageResourceName = "Password")]
        [MinLength(5)]
        public string? NewPassword { get; set; }
        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "ConfirmPassword")]
        [Compare("NewPassword")]
        public string? ConfirmPassword { get; set; }
    }
}
