using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rinsen.Outback
{
    public class TokenModel
    {
        [Required]
        [BindProperty(Name = "grant_type")]
        public string GrantType { get; set; }

        [BindProperty(Name = "code")]
        public string Code { get; set; }

        [BindProperty(Name = "redirect_uri")]
        public string RedirectUri { get; set; }

        [BindProperty(Name = "client_id")]
        public string ClientId { get; set; }

        [BindProperty(Name = "client_secret")]
        public string ClientSecret { get; set; }

        [BindProperty(Name = "code_verifier")]
        public string CodeVerifier { get; set; }
    }
}
