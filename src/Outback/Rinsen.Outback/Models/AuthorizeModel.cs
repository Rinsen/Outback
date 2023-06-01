using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rinsen.Outback.Models;

public class AuthorizeModel
{
    [Required]
    [FromQuery(Name = "client_id")]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    [FromQuery(Name = "code_challenge")]
    public string CodeChallenge { get; set; } = string.Empty;

    [FromQuery(Name = "code_challenge_method")]
    public string CodeChallengeMethod { get; set; } = string.Empty;

    [FromQuery(Name = "nonce")]
    public string Nonce { get; set; } = string.Empty;

    [FromQuery(Name = "redirect_uri")]
    public string RedirectUri { get; set; } = string.Empty;

    [Required]
    [FromQuery(Name = "response_type")]
    public string ResponseType { get; set; } = string.Empty;

    [FromQuery(Name = "response_mode")]
    public string ResponseMode { get; set; } = string.Empty;

    [FromQuery(Name = "scope")]
    public string Scope { get; set; } = string.Empty;

    [FromQuery(Name = "state")]
    public string State { get; set; } = string.Empty;
}
