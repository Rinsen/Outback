using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.App.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rinsen.Outback.App.Controllers
{
    public class DeviceController : Controller
    {
        private readonly OutbackDbContext _outbackDbContext;

        public DeviceController(OutbackDbContext outbackDbContext)
        {
            _outbackDbContext = outbackDbContext;
        }



        [HttpGet]
        [Route("device")]
        public async Task<IActionResult> Index([FromQuery(Name = "user_code")] string userCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userCode))
            {
                return View();
            }

            var deviceAuthorizationGrant = await _outbackDbContext.DeviceAuthorizationGrants.SingleOrDefaultAsync(m => m.UserCode == userCode, cancellationToken);

            if (deviceAuthorizationGrant == default)
            {
                return View();
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

        //[HttpPost]
        //[Route("device")]
        //public IActionResult IndexPost([FromForm(Name = "user_code")] string userCode)
        //{



        //    return View();
        //}

        //[HttpPost]
        //[Route("device")]
        //public async Task<IActionResult> Consent([FromForm(Name = "user_code")] string userCode)
        //{
        //    var model = new DeviceConsentViewModel
        //    {
        //        UserCode = userCode,
                
        //    };  


        //    return View();
        //}
    }
}
