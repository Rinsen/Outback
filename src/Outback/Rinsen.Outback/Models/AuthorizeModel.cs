using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rinsen.Outback.Models;

public class AuthorizeModel : IValidatableObject
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResult = new List<ValidationResult>();

        if (ResponseType != "code")
        {
            validationResult.Add(new ValidationResult($"response_type {ResponseType} is not supported", new[] { nameof(ResponseType) }));
        }

        if (string.IsNullOrEmpty(CodeChallengeMethod))
        {
            CodeChallengeMethod = "S256";
        }

        if (CodeChallengeMethod != "S256")
        {
            validationResult.Add(new ValidationResult($"code_challenge_method {CodeChallengeMethod} is not supported", new[] { nameof(CodeChallengeMethod) }));
        }

        return validationResult;
    }
}
