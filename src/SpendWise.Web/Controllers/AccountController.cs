using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendWise.Contracts.Auth;
using SpendWise.Web.ApiClient;
using SpendWise.Web.Infrastructure;
using SpendWise.Web.Models;

namespace SpendWise.Web.Controllers;

[AllowAnonymous]
public class AccountController(SpendWiseApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await SignInAsync(await api.LoginAsync(new LoginRequest(model.Username, model.Password)));
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 401 or 403)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await SignInAsync(await api.RegisterAsync(new RegisterRequest(model.Username, model.Email, model.Password)));
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 409)
        {
            ModelState.AddApiErrors(ex);
            return View(model);
        }

        TempData["Success"] = $"Welcome, {model.Username}! Your account is ready.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    /// <summary>Stores the JWT and the user's identity in the auth cookie, which expires with the token.</summary>
    private async Task SignInAsync(AuthResponse auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.User.Id.ToString()),
            new(ClaimTypes.Name, auth.User.Username),
            new(ClaimTypes.Role, auth.User.Role),
            new(AuthClaims.AccessToken, auth.Token)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTime.SpecifyKind(auth.ExpiresAt, DateTimeKind.Utc)
            });
    }
}
