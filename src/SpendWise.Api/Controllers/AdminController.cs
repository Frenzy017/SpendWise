using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendWise.Api.Infrastructure;
using SpendWise.Contracts;
using SpendWise.Contracts.Admin;
using SpendWise.Services.Interfaces;

namespace SpendWise.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)] // non-admins get 403 (FR-ADM-03)
public class AdminController(IUserAdminService userAdminService) : ControllerBase
{
    /// <summary>All users with their expense counts (no amounts) (FR-ADM-01).</summary>
    [HttpGet("users")]
    [ProducesResponseType<IReadOnlyList<AdminUserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsers() =>
        Ok(await userAdminService.GetUsersAsync());

    /// <summary>Activates or deactivates a user other than yourself (FR-ADM-02).</summary>
    [HttpPatch("users/{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(int id, SetUserStatusRequest request)
    {
        await userAdminService.SetStatusAsync(User.GetUserId(), id, request.IsActive!.Value);
        return NoContent();
    }
}
