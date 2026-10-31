using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendWise.Contracts;
using SpendWise.Web.ApiClient;

namespace SpendWise.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController(SpendWiseApiClient api) : Controller
{
    /// <summary>All accounts with activate/deactivate buttons (US-12).</summary>
    public async Task<IActionResult> Users() => View(await api.GetUsersAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, bool isActive)
    {
        try
        {
            await api.SetUserStatusAsync(id, isActive);
            TempData["Success"] = isActive ? "User activated." : "User deactivated.";
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 404)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Users));
    }
}
