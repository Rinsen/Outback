using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rinsen.IdentityProvider.Backup;
using Rinsen.IdentityProvider.Outback;
using Swashbuckle.AspNetCore.Annotations;

namespace Rinsen.Outback.App.ApiControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize("AdminsOnly")]
public class AdminController : Controller
{
    private readonly DefaultInstaller _defaultInstaller;
    private readonly IConfiguration _configuration;
    private readonly BackupGenerator _backupGenerator;

    public AdminController(DefaultInstaller defaultInstaller,
        IConfiguration configuration,
        BackupGenerator backupGenerator
        )
    {
        _defaultInstaller = defaultInstaller;
        _configuration = configuration;
        _backupGenerator = backupGenerator;
    }

    [HttpPost]
    [Route("install")]
    [SwaggerOperation(summary: "Install default clients", OperationId = "Admin_Install")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> InstallDefault()
    {
        var credentials = await _defaultInstaller.Install();

        return Ok(credentials);
    }

    [HttpGet]
    [Route("configuration")]
    [Authorize("AdminsOnly")]
    [SwaggerOperation(summary: "Get IConfiguration parameters", OperationId = "Admin_Configuration")]
    [ProducesResponseType(200)]
    public IActionResult GetConfiguration()
    {
        var result = ((IConfigurationRoot)_configuration).GetDebugView();

        return Ok(result);
    }

    [HttpGet]
    [Route("backup/download")]
    [Authorize("AdminsOnly")]
    [SwaggerOperation(summary: "Get IConfiguration parameters", OperationId = "Admin_Backup_Download")]
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
    [SwaggerOperation(summary: "Get IConfiguration parameters", OperationId = "Admin_Backup_Upload")]
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
