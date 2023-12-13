using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Configuration;
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
    private readonly IOutbackConfigurationAccessor _outbackConfigurationAccessor;
    private readonly ILogger<WellKnownController> _logger;

    public WellKnownController(IWellKnownSigningAccessor wellKnownSigningAccessor,
        IAllowedCorsOriginsAccessor allowedCorsOriginsAccessor,
        IScopeAccessor wellKnownScopeAccessor,
        IOutbackConfigurationAccessor outbackConfigurationAccessor,
        ILogger<WellKnownController> logger)
    {
        _wellKnownSigningAccessor = wellKnownSigningAccessor;
        _allowedCorsOriginsAccessor = allowedCorsOriginsAccessor;
        _wellKnownScopeAccessor = wellKnownScopeAccessor;
        _outbackConfigurationAccessor = outbackConfigurationAccessor;
        _logger = logger;
    }

    [HttpGet]
    [Route("openid-configuration")]
    public async Task<OpenIdConfiguration> OpenIdConfiguration()
    {
        await AddCorsHeadersIfRequiredAndSupported();

        var host = HttpContext.Request.Host.ToString();
        var scopes = await _wellKnownScopeAccessor.GetScopesAsync();

        var openIdConfiguration = new OpenIdConfiguration
        {
            Issuer = $"https://{host}",
            JwksUri = $"https://{host}/.well-known/openid-configuration/jwks",
            AuthorizationEndpoint = $"https://{host}/connect/authorize",
            TokenEndpoint = $"https://{host}/connect/token",
            CodeChallengeMethodsSupported = new List<string> { "S256" },
            ScopesSupported = scopes.Where(m => m.ShowInDiscoveryDocument).Select(m => m.ScopeName).ToList()
        };

        if (await _outbackConfigurationAccessor.IsClientSecretBasicAuthenticationActiveAsync())
        {
            openIdConfiguration.TokenEndpointAuthMethodsSupported.Add("client_secret_basic");
        }

        if (await _outbackConfigurationAccessor.IsClientSecretPostAuthenticationActiveAsync())
        {
            openIdConfiguration.TokenEndpointAuthMethodsSupported.Add("client_secret_post");
        }

        if (await _outbackConfigurationAccessor.IsCodeGrantActiveAsync())
        {
            openIdConfiguration.GrantTypesSupported.Add("authorization_code");
        }

        if (await _outbackConfigurationAccessor.IsDeviceAuthorizationGrantActiveAsync())
        {
            openIdConfiguration.DeviceAuthorizationEndpoint = $"https://{host}/device";
            openIdConfiguration.GrantTypesSupported.Add("urn:ietf:params:oauth:grant-type:device_code");
        }

        if (await _outbackConfigurationAccessor.IsClientCredentialsGrantActiveAsync())
        {
            openIdConfiguration.GrantTypesSupported.Add("client_credentials");
        }

        if (await _outbackConfigurationAccessor.IsRefreshTokenGrantActiveAsync())
        {
            openIdConfiguration.GrantTypesSupported.Add("refresh_token");
        }

        if (await _outbackConfigurationAccessor.IsCodeGrantActiveAsync())
        {
            openIdConfiguration.GrantTypesSupported.Add("authorization_code");
        }




        return openIdConfiguration;
    }

    private async Task AddCorsHeadersIfRequiredAndSupported()
    {
        if (Request.Headers.TryGetValue("Origin", out var origin))
        {
            var origins = await _allowedCorsOriginsAccessor.GetOrigins();

            if (origins.Contains(origin))
            {
                Response.Headers.AccessControlAllowOrigin = origin;
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
