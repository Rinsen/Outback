using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rinsen.IdentityProvider.Backup;
using Rinsen.IdentityProvider.Outback;
using Rinsen.Outback.App.Helpers;

namespace Rinsen.Outback.App.ApiControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize("AdminsOnly")]
public class AdminController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly BackupGenerator _backupGenerator;

    public AdminController(
        IConfiguration configuration,
        BackupGenerator backupGenerator
        )
    {
        _configuration = configuration;
        _backupGenerator = backupGenerator;
    }

    [HttpGet]
    [Route("configuration")]
    [Authorize("AdminsOnly")]
    [EndpointDescription("Get IConfiguration parameters")]
    [ProducesResponseType(200)]
    public IActionResult GetConfiguration()
    {
        // SEC-023: connection strings, passwords, secrets and invitation codes
        // must not reach the response — neither the keys nor their values.
        var result = ConfigurationDebugViewFilter.GetFilteredDebugView((IConfigurationRoot)_configuration);

        return Ok(result);
    }

    [HttpGet]
    [Route("backup/download")]
    [Authorize("AdminsOnly")]
    [EndpointDescription("Get IConfiguration parameters")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Download()
    {
        var backup = await _backupGenerator.CreateBackup();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var serializedBackup = JsonSerializer.Serialize(backup, options);
                
        var fileContentResult = new FileContentResult(Encoding.UTF8.GetBytes(serializedBackup), "application/octet-stream")
        {
            FileDownloadName = $"backup_{DateTime.Now:yyyy-MM-dd:HH-mm-ss}.json"
        };

        return fileContentResult;
    }

    [HttpPost("backup/upload")]
    [Authorize("AdminsOnly")]
    [EndpointDescription("Get IConfiguration parameters")]
    [ProducesResponseType(200)]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        return Ok();
    }

        //public async Task<IActionResult> Diagnostics()
        //{
        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            var disco = await httpClient.GetDiscoveryDocumentAsync(_configuration["Rinsen:InnovationBoost"]);

        //            if (disco.IsError)
        //                throw new Exception(disco.Error);

        //            var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        //            {
        //                Address = disco.TokenEndpoint,
        //                ClientId = _configuration["Rinsen:ClientId"],
        //                ClientSecret = _configuration["Rinsen:ClientSecret"]
        //            });

        //            if (tokenResponse.IsError)
        //            {
        //                throw new Exception(tokenResponse.Error);
        //            }

        //            return Ok(tokenResponse.AccessToken);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Ok(e.Message + e.StackTrace);
        //    }
        //}
    }
