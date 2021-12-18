using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ConnectController> _logger;

        public ConnectController(
            GrantService grantService,
            ClientService clientService,
            TokenFactory tokenFactory,
            ILogger<ConnectController> logger)
        {
            _grantService = grantService;
            _clientService = clientService;
            _tokenFactory = tokenFactory;
            _logger = logger;
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

                switch (model.ResponseMode)
                {
                    case "form_post":
                        // Return code in a view that is posted to the client application
                        // https://openid.net/specs/oauth-v2-form-post-response-mode-1_0.html
                        return View(new AuthorizeResponse
                        {
                            Code = code,
                            FormPostUri = model.RedirectUri,
                            Scope = model.Scope,
                            SessionState = null,
                            State = model.State
                        });
                    default:
                        // Redirect the code response as a 302 Redirect
                        return Redirect(BuildRedirectUri(model, code));
                }
            }

            // Return 400 bad request if anything is wrong
            // https://tools.ietf.org/html/draft-bradley-oauth-open-redirector-00#page-5

            return BadRequest(ModelState);
        }

        private static string BuildRedirectUri(AuthorizeModel model, string code)
        {
            var uri = model.RedirectUri;

            uri = QueryHelpers.AddQueryString(uri, "code", code);

            if (!string.IsNullOrEmpty(model.State))
            {
                uri = QueryHelpers.AddQueryString(uri, "state", model.State);
            }

            if (!string.IsNullOrEmpty(model.Scope))
            {
                uri = QueryHelpers.AddQueryString(uri, "scope", model.Scope);
            }

            return uri;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("token")]
        public async Task<IActionResult> Token([FromForm] TokenModel model)
        {
            if (ModelState.IsValid)
            {
                ClientIdentity clientIdentity;
                try
                {
                    clientIdentity = ClientIdentityHelper.GetClientIdentity(model, Request.Headers);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to get client credentials");

                    return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
                }

                Client client;
                try
                {
                    client = await _clientService.GetClient(clientIdentity);
                }
                catch (Exception)
                {
                    _logger.LogError("Client {clientId} is not found", model.ClientId);

                    return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidClient });
                }

                if (!ClientValidator.IsGrantTypeSupported(client, model.GrantType))
                {
                    _logger.LogError("Grant is not supported {grantType} for client {clientId}", model.GrantType, client.ClientId);

                    return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant });
                }

                return model.GrantType switch
                {
                    "client_credentials" => await GetTokenForClientCredentials(client),
                    "authorization_code" => await GetTokenForAuthorizationCode(model, client),
                    "refresh_token" => await GetTokenForRefreshToken(model, client),
                    _ => BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant })
                };
            }

            return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
        }

        private async Task<IActionResult> GetTokenForClientCredentials(Client client)
        {
            var tokenResponse = await _tokenFactory.CreateTokenResponse(client, GetIssuer());

            AddCacheControlHeader();

            return Json(tokenResponse);
        }

        private async Task<IActionResult> GetTokenForAuthorizationCode(TokenModel model, Client client)
        {
            CodeGrant persistedGrant;
            try
            {
                persistedGrant = await _grantService.GetCodeGrant(model.Code, client.ClientId, model.CodeVerifier);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to resolve grant for client {ClientId}", client.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant });
            }
            

            // Validate return url if provided
            if (!string.Equals(persistedGrant.RedirectUri, model.RedirectUri))
            {
                _logger.LogError("Redirect uri '{RedirectUri}' did not match granted '{GrantedRedirectUri}' redirect uri", model.RedirectUri, persistedGrant.RedirectUri);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant });
            }

            var tokenResponse = await _tokenFactory.CreateTokenResponse(client, persistedGrant, GetIssuer());

            AddCorsHeaderIfRequiredAndSupported(client);
            AddCacheControlHeader();

            return Json(tokenResponse);
        }

        private void AddCorsHeaderIfRequiredAndSupported(Client client)
        {
            if (Request.Headers.TryGetValue("Origin", out var origin))
            {
                if (client.AllowedCorsOrigins.Contains(origin))
                {
                    Response.Headers.Add("Access-Control-Allow-Origin", origin);
                }
                else
                {
                    _logger.LogError("No valid origin {origin} found for client {clientId}", origin, client.ClientId);
                }
            }
        }

        private async Task<IActionResult> GetTokenForRefreshToken(TokenModel model, Client client)
        {
            CodeGrant persistedGrant;
            try
            {
                persistedGrant = await _grantService.GetGrant(model.RefreshToken, client.ClientId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to resolve grant for client {ClientId} with {RefreshToken}", client.ClientId, model.RefreshToken);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant });
            }


            // Validate return url if provided
            if (!ClientValidator.IsRedirectUriValid(client, model.RedirectUri))
            {
                _logger.LogError("Redirect uri '{RedirectUri}' did not match granted '{GrantedRedirectUri}' redirect uri", model.RedirectUri, persistedGrant.RedirectUri);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant });
            }

            var tokenResponse = await _tokenFactory.CreateTokenResponse(client, persistedGrant, GetIssuer());

            AddCacheControlHeader();

            return Json(tokenResponse);
        }

        private void AddCacheControlHeader()
        {
            Response.Headers.Add("Cache-Control", "no-store");
            Response.Headers.Add("Pragma", "no-cache");
        }

        private string GetIssuer()
        {
            return "https://" + HttpContext.Request.Host.ToString();
        }

        // EndSession

        // userinfo

        // checksession

        // revocation

        // introspect

        // deviceauthorization

    }

    
}
