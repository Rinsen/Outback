using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rinsen.Outback.Models
{
    public class DeviceAuthorizationRequestModel
    {
        [Required]
        [FromForm(Name = "client_id")]
        public string ClientId { get; set; } = string.Empty;

        [FromForm(Name = "scope")]
        public string Scope { get; set; } = string.Empty;

    }
}
