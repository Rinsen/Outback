using Microsoft.AspNetCore.Mvc;
using Rinsen.Outback;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;
using System;
using System.Security;
using System.Threading.Tasks;

namespace Rinsen.Outback.Controllers
{
    [Route("connect")]
    public class ConnectController : Controller
    {
        private readonly GrantService _grantService;
        private readonly ClientService _clientService;
        private readonly TokenFactory _tokenFactory;

        public ConnectController(
            GrantService grantService,
            ClientService clientService,
            TokenFactory tokenFactory)
        {
            _grantService = grantService;
            _clientService = clientService;
            _tokenFactory = tokenFactory;
        }

        [HttpGet]
        [Route("authorize")]
        public async Task<IActionResult> Authorize([FromQuery]AuthorizeModel model)
        {
            if (ModelState.IsValid)
            {
                var client = await _clientService.GetClient(model);

                string code;
                if (client.ConsentRequired)
                {
                    code = await _grantService.GetCodeForExistingConsent(client, User, model);

                    if (string.IsNullOrEmpty(code))
                    {
                        return View("Consent");
                    }
                }
                else
                {
                    // Generate and store grant
                    code = await _grantService.CreateCodeAndStoreCodeGrant(client, User, model);
                }

                // Return code 
                return View(new AuthorizeResponse
                {
                    Code = code,
                    FormPostUri = model.RedirectUri,
                    Scope = model.Scope,
                    SessionState = null,
                    State = model.State
                });
            }

            return BadRequest(ModelState);
        }

        [HttpPost]
        [Route("token")]
        public async Task<IActionResult> Token([FromForm] TokenModel model)
        {
            if (ModelState.IsValid)
            {
                var clientIdentity = ClientIdentityHelper.GetClientIdentity(model, Request.Headers);

                var client = await _clientService.GetClient(clientIdentity);

                if (!ClientValidator.IsGrantTypeSupported(client, model.GrantType))
                {
                    // Client does not support grant type
                    throw new SecurityException();
                }

                return model.GrantType switch
                {
                    "client_credentials" => await GetTokenForClientCredentials(model, client),
                    "authorization_code" => await GetTokenForAuthorizationCode(model, client),
                    "refresh_token" => await GetTokenForRefreshToken(model, client),
                    _ => throw new SecurityException($"Grant {model.GrantType} is not supported"),
                };
            }

            return BadRequest(ModelState);
        }

        

        private Task<IActionResult> GetTokenForClientCredentials(TokenModel model, Client client)
        {
            //_tokenFactory.CreateTokenResponse(client)

            throw new NotImplementedException();
        }

        private async Task<IActionResult> GetTokenForAuthorizationCode(TokenModel model, Client client)
        {
            var persistedGrant = await _grantService.GetCodeGrant(model.Code, client.ClientId, model.CodeVerifier);

            // Validate return url if provided
            if (!string.Equals(persistedGrant.RedirectUri, model.RedirectUri))
            {
                throw new SecurityException($"Redirect uri '{model.RedirectUri}' did not match granted '{persistedGrant.RedirectUri}' redirect uri");
            }

            var tokenResponse = await _tokenFactory.CreateTokenResponse(User, client, persistedGrant, HttpContext.Request.Host.ToString());
            
            AddCacheControlHeader();

            return Json(tokenResponse);
        }

        private async Task<IActionResult> GetTokenForRefreshToken(TokenModel model, Client client)
        {
            var persistedGrant = await _grantService.GetGrant(model.RefreshToken, client.ClientId);

            // Validate return url if provided
            if (!ClientValidator.IsRedirectUriValid(client, model.RedirectUri))
            {
                throw new SecurityException();
            }

            var tokenResponse = await _tokenFactory.CreateTokenResponse(User, client, persistedGrant, HttpContext.Request.Host.ToString());

            AddCacheControlHeader();

            return Json(tokenResponse);
        }

        private void AddCacheControlHeader()
        {
            Response.Headers.Add("Cache-Control", "no-store");
            Response.Headers.Add("Pragma", "no-cache");
        }

        // EndSession

        // userinfo

        // checksession

        // revocation

        // introspect

        // deviceauthorization

    }

    
}
