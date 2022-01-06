using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rinsen.Outback
{
    public class TokenModel
    {
        /// <summary>
        /// Binds from grant_type
        /// </summary>
        [Required]
        [BindProperty(Name = "grant_type")]
        public string GrantType { get; set; }

        /// <summary>
        /// Binds from code
        /// </summary>
        [BindProperty(Name = "code")]
        public string Code { get; set; }

        /// <summary>
        /// Binds from refresh_token
        /// </summary>
        [BindProperty(Name = "refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Binds from redirect_uri
        /// </summary>
        [BindProperty(Name = "redirect_uri")]
        public string RedirectUri { get; set; }

        /// <summary>
        /// Binds from client_id
        /// </summary>
        [BindProperty(Name = "client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// Binds from client_secret
        /// </summary>
        [BindProperty(Name = "client_secret")]
        public string ClientSecret { get; set; }

        /// <summary>
        /// Binds from code_verifier
        /// </summary>
        [BindProperty(Name = "code_verifier")]
        public string CodeVerifier { get; set; }
    }
}
