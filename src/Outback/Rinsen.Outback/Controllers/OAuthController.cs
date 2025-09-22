using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rinsen.Outback.Controllers;

[ApiController]
[Route("oauth")]
public class OAuthController : Controller
{
    private readonly IClientService _clientService;
    private readonly IEnumerable<IGrantTypeHandler> _grantTypeHandlers;
    private readonly ILogger<OAuthController> _logger;
    private readonly IUserInfoAccessor _userInfoAccessor;

    public OAuthController(
        IClientService clientService,
        IEnumerable<IGrantTypeHandler> grantTypeHandlers,
        ILogger<OAuthController> logger,
        IUserInfoAccessor userInfoAccessor)
    {
        _clientService = clientService;
        _grantTypeHandlers = grantTypeHandlers;
        _logger = logger;
        _userInfoAccessor = userInfoAccessor;
    }

    [HttpGet]
    [Route("authorize")]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeModel model)
    {
        if (ModelState.IsValid)
        {
            var client = await _clientService.GetClient(model.ClientId);

            var grantTypeHandler = _grantTypeHandlers.SingleOrDefault(h => h.GrantType == "authorization_code");

            if (grantTypeHandler is not AuthorizationCodeGrantTypeHandler authorizationCodeGrantHandler)
            {
                _logger.LogWarning("Grant type {ResponseType} is not active for client {ClientId}", model.ResponseType, client.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
            }

            if (model.ResponseType != "code")
            {
                _logger.LogWarning("Response type {ResponseType} is not valid for client {ClientId}", model.ResponseType, client.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
            }

            if (string.IsNullOrEmpty(model.CodeChallengeMethod))
            {
                model.CodeChallengeMethod = "S256";
            }

            if (model.CodeChallengeMethod != "S256")
            {
                _logger.LogWarning("code_challenge_method {CodeChallengeMethod} is not supported for client {ClientId}", model.ResponseType, client.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
            }

            if (!ClientValidator.IsScopeValid(client, model.Scope))
            {
                _logger.LogWarning("Client scopes {Scope} is not valid for client {ClientId}", model.Scope, client.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidScope });
            }

            if (!ClientValidator.IsRedirectUriValid(client, model.RedirectUri))
            {
                _logger.LogWarning("Redirect uri {RedirectUri} is not valid for {ClientId}", model.RedirectUri, client.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
            }

            string code;
            if (client.ConsentRequired)
            {
                code = await authorizationCodeGrantHandler.GetCodeForExistingConsentAsync(client, User, model);

                if (string.IsNullOrEmpty(code))
                {
                    return View("Consent");
                }
            }
            else
            {
                // Generate and store grant
                code = await authorizationCodeGrantHandler.CreateCodeAndStoreCodeGrantAsync(client, User, model);
            }

            // If no redirect_uri is provided but the client have only one redirect uri we use that uri (2.3.3).
            if (string.IsNullOrEmpty(model.RedirectUri))
            {
                model.RedirectUri = client.LoginRedirectUris.Single();
            }

            return model.ResponseMode switch
            {
                // Return code in a view that is posted to the client application
                // https://openid.net/specs/oauth-v2-form-post-response-mode-1_0.html
                "form_post" => View(new AuthorizeResponse
                {
                    Code = code,
                    FormPostUri = model.RedirectUri,
                    Scope = model.Scope,
                    SessionState = null,
                    State = model.State
                }),
                _ => Redirect(BuildRedirectUri(model, code)),// Redirect the code response as a 302 Redirect
            };
        }

        var errorString = string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));

        _logger.LogWarning("Model validation failed for {ClientId} with errors {Errors}", model.ClientId, errorString);

        return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
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

            var grantTypeHandler = _grantTypeHandlers.SingleOrDefault(h => h.GrantType == model.GrantType);

            if (grantTypeHandler == default)
            {
                _logger.LogError("No grant type handler found for grant type {grantType}", model.GrantType);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant });
            }

            var result = await grantTypeHandler.GetTokenAsync(model, client, GetIssuer());

            if (result.IsError)
            {
                return BadRequest(result.ErrorResponse);
            }
            
            AddCorsHeaderIfRequiredAndSupported(client);
            AddCacheControlHeader();

            return Json(result.AccessTokenResponse);
        }

        return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
    }

    [HttpPost]
    [Route("device_authorization")]
    public async Task<IActionResult> DeviceAuthorization(DeviceAuthorizationRequestModel deviceAuthorizationModel)
    {
        if (ModelState.IsValid)
        {
            var client = await _clientService.GetClient(deviceAuthorizationModel.ClientId);

            if (!client.SupportedGrantTypes.Any(g => g == "urn:ietf:params:oauth:grant-type:device_code"))
            {
                _logger.LogWarning("Client {ClientId} does not support device authorization code grant", deviceAuthorizationModel.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant });
            }

            var scope = string.Empty;
            if (string.IsNullOrEmpty(deviceAuthorizationModel.Scope))
            {
                scope = string.Join(' ', client.Scopes);
            }
            else if (!ClientValidator.IsScopeValid(client, deviceAuthorizationModel.Scope))
            {
                _logger.LogWarning("Client scopes {Scope} is not valid for client {ClientId}", deviceAuthorizationModel.Scope, client.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidScope });
            }
            else
            {
                scope = deviceAuthorizationModel.Scope;
            }

            var grantTypeHandler = _grantTypeHandlers.SingleOrDefault(h => h.GrantType == "urn:ietf:params:oauth:grant-type:device_code");

            if (grantTypeHandler is not DeviceCodeGrantTypeHandler deviceCodeGrantTypeHandler)
            {
                _logger.LogWarning("Grant type device code is not active for client {ClientId}", client.ClientId);

                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
            }

            var deviceAuthorizationGrantRequest = await deviceCodeGrantTypeHandler.GetDeviceCodeGrantAsync(client, scope);

            if (deviceAuthorizationGrantRequest == default)
            {
                return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
            }

            return Json(new DeviceAuthorizationResponse
            {
                DeviceCode = deviceAuthorizationGrantRequest.DeviceCode,
                UserCode = deviceAuthorizationGrantRequest.UserCode,
                ExpiresIn = client.DeviceCodeUserCompletionLifetime,
                Interval = deviceAuthorizationGrantRequest.Interval,
                VerificationUri = $"https://{HttpContext.Request.Host}/device",
                VerificationUriComplete = $"https://{HttpContext.Request.Host}/device?user_code={deviceAuthorizationGrantRequest.UserCode}"
            });
        }

        return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
    }

    [HttpGet]
    [HttpPost]
    [Authorize]
    [Route("userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        if (!(User?.Identity?.IsAuthenticated ?? false))
        {
            return Unauthorized();
        }

        var subjectId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subjectId))
        {
            _logger.LogWarning("No subject (sub) claim found on principal for userinfo request");
            return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest, ErrorDescription = "Missing subject" });
        }

        var scopeClaim = User.FindFirst("scope")?.Value;
        var scopes = new List<string>();
        if (!string.IsNullOrEmpty(scopeClaim))
        {
            scopes = scopeClaim.Split(' ').ToList();
        }
        else
        {
            _logger.LogWarning("No scope claim found on principal for userinfo request for subject {SubjectId}", subjectId);
        }

        Dictionary<string, string> userInfoClaims;
        try
        {
            userInfoClaims = await _userInfoAccessor.GetUserInfoClaims(subjectId, scopes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get userinfo claims for subject {SubjectId}", subjectId);
            return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidRequest });
        }

        // Ensure 'sub' is always included per OIDC spec
        userInfoClaims["sub"] = subjectId;

        AddCacheControlHeader();

        return Json(userInfoClaims);
    }

    private void AddCorsHeaderIfRequiredAndSupported(Client client)
    {
        if (Request.Headers.TryGetValue("Origin", out var origin))
        {
            if (StringValues.IsNullOrEmpty(origin))
            {
                _logger.LogInformation("No content of Origin header found");
            }
            else
            {
                if (client.AllowedCorsOrigins.Contains(origin.ToString()))
                {
                    Response.Headers.AccessControlAllowOrigin = origin;
                }
                else
                {
                    _logger.LogError("No valid origin {origin} found for client {clientId}", origin.ToString(), client.ClientId);
                }
            }
        }
    }

    private void AddCacheControlHeader()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
    }

    private string GetIssuer()
    {
        return "https://" + HttpContext.Request.Host.ToString();
    }



}
