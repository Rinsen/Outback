using System.ComponentModel.DataAnnotations;

namespace Rinsen.Outback.App.Models;

public class CreateUser
{
    [Required]
    public string GivenName { get; set; } = string.Empty;

    [Required]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
