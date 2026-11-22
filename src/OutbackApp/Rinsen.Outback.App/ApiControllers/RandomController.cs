using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rinsen.IdentityProvider;

namespace Rinsen.Outback.App.ApiControllers;

[ApiController]
[Route("Outback/api/[controller]")]
[Authorize(Policy = "AdminsOnly")]
public class RandomController : Controller
{
    private readonly RandomStringGenerator _randomStringGenerator;

    public RandomController(RandomStringGenerator randomStringGenerator)
    {
        _randomStringGenerator = randomStringGenerator;
    }

    [HttpGet("{count}")]
    [EndpointDescription("Get a random string with provided length")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public ActionResult<string> Get(int count)
    {
        return _randomStringGenerator.GetRandomString(count);
    }
}
