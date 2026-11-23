namespace Rinsen.Outback.App.Models;

public class EnableTotpResult
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
}
