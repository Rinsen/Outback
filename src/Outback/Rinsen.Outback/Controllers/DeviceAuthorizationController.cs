using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Controllers
{
    public class DeviceAuthorizationController : Controller
    {
        private readonly IClientAccessor _clientAccessor;
        private readonly IEnumerable<IGrantTypeHandler> _grantTypeHandlers;
        private readonly ILogger<DeviceAuthorizationController> _logger;

        public DeviceAuthorizationController(IClientAccessor clientAccessor,
            IEnumerable<IGrantTypeHandler> grantTypeHandlers,
            ILogger<DeviceAuthorizationController> logger)
        {
            _clientAccessor = clientAccessor;
            _grantTypeHandlers = grantTypeHandlers;
            _logger = logger;
        }


        [HttpPost]
        [Route("device_authorization")]
        public async Task<IActionResult> DeviceAuthorization(DeviceAuthorizationRequestModel deviceAuthorizationModel)
        {
            if (ModelState.IsValid)
            {
                var client = await _clientAccessor.GetClient(deviceAuthorizationModel.ClientId);

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
    }
}
