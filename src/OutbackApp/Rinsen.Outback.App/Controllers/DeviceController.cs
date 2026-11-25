using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.App.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rinsen.Outback.App.Controllers
{
    public class DeviceController : Controller
    {
        private readonly OutbackDbContext _outbackDbContext;
        private readonly IIdentityAccessor _identityAccessor;

        public DeviceController(OutbackDbContext outbackDbContext, IIdentityAccessor identityAccessor)
        {
            _outbackDbContext = outbackDbContext;
            _identityAccessor = identityAccessor;
        }



        [HttpGet]
        [Route("device")]
        public async Task<IActionResult> Index([FromQuery(Name = "user_code")] string userCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userCode))
            {
                return View(new DeviceConsentViewModel());
            }

            var deviceAuthorizationGrant = await _outbackDbContext.DeviceAuthorizationGrants.SingleOrDefaultAsync(m => m.UserCode == userCode, cancellationToken);

            if (deviceAuthorizationGrant == default)
            {
                return View(new DeviceConsentViewModel { UserCode = userCode });
            }

            if (deviceAuthorizationGrant.UserCodeExpiration < System.DateTimeOffset.UtcNow)
            {
                return View("UserCodeExpired");
            }

            var scopeNames = deviceAuthorizationGrant.Scope.Split(" ");

            var scopes = await _outbackDbContext.Scopes.Where(m => scopeNames.Contains(m.ScopeName)).ToListAsync(cancellationToken);

            var model = new DeviceConsentViewModel
            {
                UserCode = userCode,
                Scopes = scopes.Where(scope => scope.Enabled).Select(scope => new DeviceConsentScopeModel { Name = scope.ScopeName, Description = scope.Description, DisplayName = scope.DisplayName }).ToList()
            };

            return View("Consent", model);
        }

        /// <summary>
        /// FUNC-003: the device consent decision. Authorizing binds the grant to the
        /// authenticated user (SubjectId), which is what the token endpoint
        /// (DeviceCodeGrantTypeHandler) checks before issuing a token; rejecting sets
        /// AccessIsRejected so the token endpoint answers access_denied.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Consent([FromForm] DeviceConsentViewModel model, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(model?.UserCode))
            {
                return View("Index", new DeviceConsentViewModel());
            }

            var deviceAuthorizationGrant = await _outbackDbContext.DeviceAuthorizationGrants.SingleOrDefaultAsync(m => m.UserCode == model.UserCode, cancellationToken);

            if (deviceAuthorizationGrant == default)
            {
                return View("Index", new DeviceConsentViewModel { UserCode = model.UserCode });
            }

            if (deviceAuthorizationGrant.UserCodeExpiration < DateTimeOffset.UtcNow)
            {
                return View("UserCodeExpired");
            }

            if (deviceAuthorizationGrant.Resolved is not null)
            {
                // The device code has already been redeemed, so there is nothing left to consent to.
                return View("UserCodeExpired");
            }

            if (deviceAuthorizationGrant.SubjectId != Guid.Empty)
            {
                // Already consented — keep the original subject, do not overwrite.
                return View("ConsentGranted");
            }

            if (deviceAuthorizationGrant.AccessIsRejected)
            {
                return View("ConsentDenied");
            }

            if (model.Authorize)
            {
                deviceAuthorizationGrant.SubjectId = _identityAccessor.IdentityId;
                deviceAuthorizationGrant.Accepted = DateTimeOffset.UtcNow;
                deviceAuthorizationGrant.AccessIsRejected = false;

                await _outbackDbContext.SaveChangesAsync(cancellationToken);

                return View("ConsentGranted");
            }

            deviceAuthorizationGrant.AccessIsRejected = true;

            await _outbackDbContext.SaveChangesAsync(cancellationToken);

            return View("ConsentDenied");
        }
    }
}
