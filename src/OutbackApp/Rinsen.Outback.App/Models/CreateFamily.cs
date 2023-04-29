using System.ComponentModel.DataAnnotations;

namespace Rinsen.Outback.App.Models;

public class CreateFamily
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;
}
