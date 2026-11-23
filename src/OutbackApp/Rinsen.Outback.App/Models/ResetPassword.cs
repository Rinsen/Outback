using System.ComponentModel.DataAnnotations;

namespace Rinsen.Outback.App.Models;

public class ResetPassword
{
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
