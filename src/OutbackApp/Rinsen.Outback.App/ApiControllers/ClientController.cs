using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rinsen.IdentityProvider.Outback;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.App.Models;
using Rinsen.Outback.Configuration;

namespace Rinsen.Outback.App.ApiControllers;

[ApiController]
[Route("Outback/api/[controller]")]
[Authorize(Policy = "AdminsOnly")]
public class ClientController : Controller
{
    private readonly ClientService _clientService;
    private readonly IOutbackConfigurationAccessor _outbackConfigurationAccessor;

    public ClientController(ClientService clientService, IOutbackConfigurationAccessor outbackConfigurationAccessor)
    {
        _clientService = clientService;
        _outbackConfigurationAccessor = outbackConfigurationAccessor;
    }

    [HttpGet]
    [EndpointDescription("Get a list of all clients")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<List<OutbackClient>>> GetAll()
    {
        var clients = await _clientService.GetAllAsync();

        clients.ForEach(c => c.Secrets.ForEach(s => s.Secret = "****"));

        return clients;
    }

    [HttpGet("{id}")]
    [EndpointDescription("Get a specific client by id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OutbackClient>> GetById(string id)
    {
        var client = await _clientService.GetClientAsync(id);

        if (client == default)
            return NotFound();

        client.Secrets.ForEach(s => s.Secret = "****");

        return client;
    }

    [HttpDelete("{id}")]
    [EndpointDescription("Delete a client")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(string id)
    {
        await _clientService.DeleteClientAsync(id);

        return Ok();
    }

    [HttpPost]
    [EndpointDescription("Create a client")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OutbackClient>> Create([Required] CreateClient createClient)
    {
        var clientId = Guid.NewGuid().ToString();

        await _clientService.CreateNewClientAsync(clientId, createClient.ClientName, createClient.Description, createClient.FamilyId, createClient.ClientType);

        var client = await _clientService.GetClientAsync(clientId);

        return CreatedAtAction(nameof(GetById),
           new { id = clientId }, client);
    }

    [HttpPut("{id}")]
    [EndpointDescription("Update a client")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> Update(string id, [FromBody][Required] OutbackClient client)
    {
        await _clientService.UpdateClientAsync(id, client);

        return Ok();
    }

    [HttpGet]
    [EndpointDescription("Get all client families")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [Route("~/Outback/api/[controller]/Family")]
    public async Task<ActionResult<List<OutbackClientFamily>>> GetAllClientFamilies()
    {
        var clientFamilies = await _clientService.GetAllClientFamiliesAsync();

        return clientFamilies;
    }

    [HttpPost]
    [EndpointDescription("Create client family")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [Route("~/Outback/api/[controller]/Family")]
    public async Task<ActionResult<OutbackClientFamily>> CreateFamily([Required] CreateFamily createFamily)
    {
        var family = await _clientService.CreateNewFamilyAsync(createFamily.Name, createFamily.Description);

        return CreatedAtAction(nameof(GetById),
           new { id = family.Id }, family);
    }

    [HttpGet]
    [Route("~/Outback/api/[controller]/GrantTypes")]
    [EndpointDescription("Get all available grant types")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<List<string>>> GetAvailableGrantTypes()
    {
        var grantTypes = new List<string>();

        if (await _outbackConfigurationAccessor.IsCodeGrantActiveAsync())
        {
            grantTypes.Add("authorization_code");
        }

        if (await _outbackConfigurationAccessor.IsDeviceAuthorizationGrantActiveAsync())
        {
            grantTypes.Add("urn:ietf:params:oauth:grant-type:device_code");
        }

        if (await _outbackConfigurationAccessor.IsClientCredentialsGrantActiveAsync())
        {
            grantTypes.Add("client_credentials");
        }

        if (await _outbackConfigurationAccessor.IsRefreshTokenGrantActiveAsync())
        {
            grantTypes.Add("refresh_token");
        }

        return grantTypes;
    }
}
