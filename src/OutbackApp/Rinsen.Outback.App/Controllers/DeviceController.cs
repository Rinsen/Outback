using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Rinsen.Outback.App.Controllers
{
    public class DeviceController
    {

        [HttpGet]
        [Route("device")]
        public async Task<IActionResult> Index([FromQuery(Name = "user_code")] string userCode)
        {



            return View();
        }

    }
}
