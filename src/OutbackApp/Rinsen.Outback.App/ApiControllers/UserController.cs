using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rinsen.IdentityProvider;
using Rinsen.Outback.App.Models;

namespace Rinsen.Outback.App.ApiControllers;

[ApiController]
[Route("Outback/api/[controller]")]
[Authorize(Policy = "AdminsOnly")]
[TypeFilter(typeof(UserExceptionFilter))]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IdentityOptions _identityOptions;

    public UserController(IUserService userService, IdentityOptions identityOptions)
    {
        _userService = userService;
        _identityOptions = identityOptions;
    }

    [HttpGet]
    [EndpointDescription("Get a list of all users")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        return await _userService.GetAllAsync();
    }

    [HttpGet("{id}")]
    [EndpointDescription("Get a specific user by identity id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        return await _userService.GetAsync(id);
    }

    [HttpPost]
    [EndpointDescription("Create a user with a local account")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<User>> Create([Required] CreateUser createUser)
    {
        var user = await _userService.CreateAsync(createUser.GivenName, createUser.Surname, createUser.Email, createUser.PhoneNumber, createUser.Password);

        return CreatedAtAction(nameof(GetById),
            new { id = user.IdentityId }, user);
    }

    [HttpPut("{id}")]
    [EndpointDescription("Update a user")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Update(Guid id, [FromBody][Required] UpdateUser updateUser)
    {
        await _userService.UpdateAsync(id, updateUser.GivenName, updateUser.Surname, updateUser.Email, updateUser.PhoneNumber, updateUser.IsAdministrator, updateUser.IsDisabled);

        return Ok();
    }

    [HttpPost("{id}/Password")]
    [EndpointDescription("Reset the password of a user")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> ResetPassword(Guid id, [FromBody][Required] ResetPassword resetPassword)
    {
        await _userService.ResetPasswordAsync(id, resetPassword.Password);

        return Ok();
    }

    [HttpPost("{id}/Totp/Enable")]
    [EndpointDescription("Enable TOTP two-factor authentication for a user and return the base32 secret")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EnableTotpResult>> EnableTotp(Guid id)
    {
        var secret = await _userService.EnableTotpAsync(id);

        return Ok(new EnableTotpResult { Secret = secret, Issuer = _identityOptions.TotpIssuer });
    }

    [HttpPost("{id}/Totp/Disable")]
    [EndpointDescription("Disable TOTP two-factor authentication for a user")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DisableTotp(Guid id)
    {
        await _userService.DisableTotpAsync(id);

        return Ok();
    }

    [HttpDelete("{id}")]
    [EndpointDescription("Delete a user (soft delete)")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _userService.DeleteAsync(id);

        return Ok();
    }
}
