using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Abstractons;
using Rinsen.Outback.Models;
using Rinsen.Outback.WellKnown;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rinsen.Outback.Controllers
{
    [ApiController]
    [Route(".well-known")]
    public class WellKnownController : ControllerBase    
    {
        private readonly IWellKnownSigningAccessor _wellKnownSigningAccessor;
        private readonly IAllowedCorsOriginsAccessor _allowedCorsOriginsAccessor;
        private readonly IWellKnownScopeAccessor _wellKnownScopeAccessor;
        private readonly ILogger<WellKnownController> _logger;

        public WellKnownController(IWellKnownSigningAccessor wellKnownSigningAccessor,
            IAllowedCorsOriginsAccessor allowedCorsOriginsAccessor,
            IWellKnownScopeAccessor wellKnownScopeAccessor,
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
            var scopes = await _wellKnownScopeAccessor.GetScopes();

            return new OpenIdConfiguration
            {
                Issuer = host,
                JwksUri = $"https://{host}/.well-known/openid-configuration/jwks",
                AuthorizationEndpoint = $"https://{host}/connect/authorize",
                TokenEndpoint = $"https://{host}/connect/token",
                TokenEndpointAuthMethodsSupported = new List<string> { "client_secret_basic" },
                GrantTypesSupported = new List<string> { "authorization_code", "client_credentials", "refresh_token" },
                CodeChallengeMethodsSupported = new List<string> { "S256" },
                FrontchannelLogoutSessionSupported = false,
                FrontchannelLogoutSupported = false,
                BackchannelLogoutSessionSupported = false,
                BackchannelLogoutSupported = false,
                ScopesSupported = scopes
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

        [Route("openid-configuration/jwks")]
        public async Task<EllipticCurveJsonWebKeyModelKeys> OpenIdConfigurationJwks()
        {
            var keyModel = await _wellKnownSigningAccessor.GetEllipticCurveJsonWebKeyModelKeys();

            return keyModel;
        }
    }

    
}
