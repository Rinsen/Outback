using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rinsen.Outback.Models;

public class AuthorizeModel
{
    /// <summary>
    /// Gets or sets the client identifier provided in the query string.
    /// </summary>
    /// <remarks>This property is required and must be provided as a query parameter named
    /// <c>client_id</c>.</remarks>
    [Required]
    [FromQuery(Name = "client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the code challenge used for PKCE (Proof Key for Code Exchange) authentication.
    /// </summary>
    /// <remarks>This property is required and must be provided as a query parameter named
    /// <c>code_challenge</c>.</remarks>
    [Required]
    [FromQuery(Name = "code_challenge")]
    public string CodeChallenge { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the code challenge method used in the authorization process.
    /// </summary>
    [FromQuery(Name = "code_challenge_method")]
    public string CodeChallengeMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for the request, used to prevent replay attacks.
    /// </summary>
    [FromQuery(Name = "nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URI to which the user will be redirected after completing the operation.
    /// </summary>
    [FromQuery(Name = "redirect_uri")]
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response type parameter for the request.
    /// </summary>
    [Required]
    [FromQuery(Name = "response_type")]
    public string ResponseType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response mode used to determine how the response data is formatted.
    /// </summary>
    [FromQuery(Name = "response_mode")]
    public string ResponseMode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scope of the query, typically used to specify the context or range of the operation.
    /// </summary>
    [FromQuery(Name = "scope")]
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state parameter from the query string.
    /// </summary>
    [FromQuery(Name = "state")]
    public string State { get; set; } = string.Empty;
}
