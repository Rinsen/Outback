using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rinsen.Outback;

/// <summary>
/// SEC-016: shared rate-limiting policy names. The <c>Credentials</c> policy is
/// applied to every credential endpoint (<c>/Identity/Login</c>,
/// <c>/Identity/TwoFactor</c>, <c>/Identity/Create</c> and <c>/oauth/token</c>)
/// so that brute-force and credential-stuffing attempts are throttled.
/// </summary>
public static class RateLimitingPolicies
{
    public const string Credentials = "credentials";
}

/// <summary>
/// SEC-016: registers the credential-endpoint rate limiter. The limits are
/// configurable under the <c>Rinsen:RateLimiting</c> configuration section
/// (defaults: 10 requests per 60-second window) and rejected requests receive
/// HTTP 429 (Too Many Requests).
/// </summary>
public static class RateLimitingExtensions
{
    public const string ConfigurationSection = "Rinsen:RateLimiting";
    public const int DefaultPermitLimit = 10;
    public const int DefaultWindowSeconds = 60;

    public static IServiceCollection AddCredentialRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationSection);
        var permitLimit = section.GetValue<int>("PermitLimit", DefaultPermitLimit);
        var windowSeconds = section.GetValue<int>("WindowSeconds", DefaultWindowSeconds);

        if (permitLimit <= 0)
        {
            permitLimit = DefaultPermitLimit;
        }

        if (windowSeconds <= 0)
        {
            windowSeconds = DefaultWindowSeconds;
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(RateLimitingPolicies.Credentials, limiter =>
            {
                limiter.PermitLimit = permitLimit;
                limiter.Window = TimeSpan.FromSeconds(windowSeconds);
                limiter.QueueLimit = 0;
            });
        });

        return services;
    }
}
