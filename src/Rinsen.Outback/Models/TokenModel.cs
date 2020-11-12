using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rinsen.Outback
{
    public class TokenModel : IValidatableObject
    {

        [Required]
        [BindProperty(Name = "grant_type")]
        public string GrantType { get; set; }

        [Required]
        [BindProperty(Name = "code")]
        public string Code { get; set; }

        [BindProperty(Name = "redirect_uri")]
        public string RedirectUri { get; set; }

        [Required]
        [BindProperty(Name = "client_id")]
        public string ClientId { get; set; }

        [BindProperty(Name = "client_secret")]
        public string ClientSecret { get; set; }

        [Required]
        [BindProperty(Name = "code_verifier")]
        public string CodeVerifier { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResult = new List<ValidationResult>();

            if (GrantType != "authorization_code")
            {
                validationResult.Add(new ValidationResult($"grant_type must be set to authorization_code", new[] { nameof(GrantType) }));
            }

            return validationResult;
        }
    }
}
