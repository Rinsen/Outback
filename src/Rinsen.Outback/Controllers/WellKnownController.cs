using Microsoft.AspNetCore.Mvc;
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
        private readonly IWellKnownSigningStorage _wellKnownSigningStorage;

        public WellKnownController(IWellKnownSigningStorage wellKnownSigningStorage)
        {
            _wellKnownSigningStorage = wellKnownSigningStorage;
        }

        [Route("openid-configuration")]
        public OpenIdConfiguration OpenIdConfiguration()
        {
            var host = HttpContext.Request.Host.ToString();

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
            };
        }

        [Route("openid-configuration/jwks")]
        public async Task<EllipticCurveJsonWebKeyModelKeys> OpenIdConfigurationJwks()
        {
            var keyModel = await _wellKnownSigningStorage.GetEllipticCurveJsonWebKeyModelKeys();

            return keyModel;
        }
    }

    
}
