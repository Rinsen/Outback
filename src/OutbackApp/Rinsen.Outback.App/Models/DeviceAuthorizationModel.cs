using System.ComponentModel.DataAnnotations;

namespace Rinsen.Outback.App.Models;

public class DeviceAuthorizationModel
{
    [Required]
    public string UserCode { get; set; } = string.Empty;

    public bool Authorize { get; set; } = false;

}
