﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BSharpUnilever.Controllers.ViewModels.Auth
{
    /// <summary>
    /// Represents the incoming credentials of the user
    /// </summary>
    public class CreateTokenVM
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

        [Required]
        [StringLength(256)]
        public string Password { get; set; }
    }
}
