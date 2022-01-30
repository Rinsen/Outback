namespace Rinsen.Outback.Models;

public class AuthorizeResponse
{
    public string Code { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? SessionState { get; set; } = null;

    public string FormPostUri { get; set; } = string.Empty;

}
