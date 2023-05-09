using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Models;
using Rinsen.Outback.WellKnown;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rinsen.Outback.Controllers;

[ApiController]
[AllowAnonymous]
[Route(".well-known")]
public class WellKnownController : ControllerBase
{
    private readonly IWellKnownSigningAccessor _wellKnownSigningAccessor;
    private readonly IAllowedCorsOriginsAccessor _allowedCorsOriginsAccessor;
    private readonly IScopeAccessor _wellKnownScopeAccessor;
    private readonly ILogger<WellKnownController> _logger;

    public WellKnownController(IWellKnownSigningAccessor wellKnownSigningAccessor,
        IAllowedCorsOriginsAccessor allowedCorsOriginsAccessor,
        IScopeAccessor wellKnownScopeAccessor,
        ILogger<WellKnownController> logger)
    {
        _wellKnownSigningAccessor = wellKnownSigningAccessor;
        _allowedCorsOriginsAccessor = allowedCorsOriginsAccessor;
        _wellKnownScopeAccessor = wellKnownScopeAccessor;
        _logger = logger;
    }

    [HttpGet]
    [Route("openid-configuration")]
    public async Task<OpenIdConfiguration> OpenIdConfiguration()
    {
        await AddCorsHeadersIfRequiredAndSupported();

        var host = HttpContext.Request.Host.ToString();
        var scopes = await _wellKnownScopeAccessor.GetScopesAsync();

        return new OpenIdConfiguration
        {
            Issuer = $"https://{host}",
            JwksUri = $"https://{host}/.well-known/openid-configuration/jwks",
            AuthorizationEndpoint = $"https://{host}/connect/authorize",
            DeviceAuthorizationEndpoint = $"https://{host}/device",
            TokenEndpoint = $"https://{host}/connect/token",
            TokenEndpointAuthMethodsSupported = new List<string> { "client_secret_basic" },
            GrantTypesSupported = new List<string> { "authorization_code", "client_credentials", "refresh_token", "urn:ietf:params:oauth:grant-type:device_code" },
            CodeChallengeMethodsSupported = new List<string> { "S256" },
            FrontchannelLogoutSessionSupported = false,
            FrontchannelLogoutSupported = false,
            BackchannelLogoutSessionSupported = false,
            BackchannelLogoutSupported = false,
            ScopesSupported = scopes.Where(m => m.ShowInDiscoveryDocument).Select(m => m.ScopeName).ToList()
        };
    }

    private async Task AddCorsHeadersIfRequiredAndSupported()
    {
        if (Request.Headers.TryGetValue("Origin", out var origin))
        {
            var origins = await _allowedCorsOriginsAccessor.GetOrigins();

            if (origins.Contains(origin))
            {
                Response.Headers.Add("Access-Control-Allow-Origin", origin);
            }
            else
            {
                _logger.LogInformation("No valid origin found for {origin}", origin);
            }
        }
    }

    [HttpGet]
    [Route("openid-configuration/jwks")]
    public async Task<EllipticCurveJsonWebKeyModelKeys> OpenIdConfigurationJwks()
    {
        await AddCorsHeadersIfRequiredAndSupported();

        var keyModel = await _wellKnownSigningAccessor.GetEllipticCurveJsonWebKeyModelKeys();

        return keyModel;
    }
}
