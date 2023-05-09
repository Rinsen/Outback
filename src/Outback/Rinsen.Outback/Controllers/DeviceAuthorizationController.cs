using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Controllers
{
    public class DeviceAuthorizationController : Controller
    {
        private readonly IClientAccessor _clientAccessor;
        private readonly IGrantService _grantService;

        public DeviceAuthorizationController(IClientAccessor clientAccessor,
            IGrantService grantService)
        {
            _clientAccessor = clientAccessor;
            _grantService = grantService;
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
                    return BadRequest(new ErrorResponse { Error = ErrorResponses.InvalidGrant });
                }

                var deviceAuthorizationGrantRequest = await _grantService.GetDeviceAuthorizationGrantAsync(client);

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
