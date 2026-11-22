using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rinsen.IdentityProvider.Outback;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.App.ApiModels;

namespace Rinsen.Outback.App.ApiControllers;

[Route("Outback/api/[controller]")]
[Authorize(Policy = "AdminsOnly")]
[ApiController]
public class ScopeController : ControllerBase
{
    private readonly ScopeService _scopeService;

    public ScopeController(ScopeService scopeService)
    {
        _scopeService = scopeService;
    }

    // GET: api/<ScopeController>
    [HttpGet]
    [EndpointDescription("Get all scopes")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IEnumerable<OutbackScope>> Get()
    {
        var clients = await _scopeService.GetAllAsync();

        return clients;
    }

    // GET api/<ScopeController>/5
    [HttpGet("{id}")]
    [EndpointDescription("Get a specific scope by id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OutbackScope>> GetById(int id)
    {
        var scope = await _scopeService.GetScopeAsync(id);

        if (scope == default)
            return NotFound();

        return scope;
    }

    // POST api/<ScopeController>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(403)]
    [EndpointDescription("Create a new scope")]
    public async Task<ActionResult<OutbackScope>> Post([FromBody]CreateScope createScope)
    {
        var scopeId = await _scopeService.CreateScopeAsync(createScope.Name, createScope.ScopeName, createScope.Audience, createScope.Description, createScope.ShowInDiscoveryDocument, createScope.ClaimsInIdToken, createScope.Enabled);

        var scope = await _scopeService.GetScopeAsync(scopeId);

        return CreatedAtAction(nameof(GetById),
           new { id = scopeId }, scope);
    }

    // PUT api/<ScopeController>/5
    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [EndpointDescription("Update scope")]
    public async Task<ActionResult> Put(int id, [FromBody] OutbackScope scope)
    {
        await _scopeService.UpdateScopeAsync(id, scope);

        return Ok();
    }

    // DELETE api/<ScopeController>/5
    [HttpDelete("{id}")]
    [EndpointDescription("Delete a scope")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> DeleteAsync(int id)
    {
        await _scopeService.DeleteScopeAsync(id);

        return Ok();
    }
}
