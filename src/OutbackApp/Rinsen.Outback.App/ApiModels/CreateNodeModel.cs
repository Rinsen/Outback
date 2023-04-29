using System.ComponentModel.DataAnnotations;

namespace Rinsen.Outback.App.ApiModels;

public class CreateNodeModel
{
    [Required]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    public string ClientDescription { get; set; } = string.Empty;

}
