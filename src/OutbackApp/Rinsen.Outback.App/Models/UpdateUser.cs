using System.ComponentModel.DataAnnotations;

namespace Rinsen.Outback.App.Models;

public class UpdateUser
{
    [Required]
    public string GivenName { get; set; } = string.Empty;

    [Required]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsAdministrator { get; set; }

    public bool IsDisabled { get; set; }
}
