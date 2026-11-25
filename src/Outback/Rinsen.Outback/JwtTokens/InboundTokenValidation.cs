using System;
using Microsoft.IdentityModel.Tokens;

namespace Rinsen.Outback.JwtTokens;

/// <summary>
/// Centralizes the inbound JWT bearer token validation settings so that consuming
/// applications keep a single, testable source of truth. Only ES256 (ECDSA P-256)
/// signatures are accepted, which matches the algorithm the Outback issuer signs with.
/// </summary>
public static class InboundTokenValidation
{
    /// <summary>
    /// Creates the explicit <see cref="TokenValidationParameters"/> used to validate
    /// inbound access tokens.
    /// </summary>
    public static TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 },
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }
}
