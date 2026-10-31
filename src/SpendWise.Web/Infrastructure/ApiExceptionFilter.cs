using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SpendWise.Web.ApiClient;

namespace SpendWise.Web.Infrastructure;

/// <summary>
/// Handles API errors that a controller didn't handle itself (FR-WEB-03):
/// 401 logs the user out and sends them to the login page; anything else shows a friendly error page.
/// </summary>
public class ApiExceptionFilter(IModelMetadataProvider metadataProvider) : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiException ex)
            return;

        if (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        }
        else
        {
            context.Result = new ViewResult
            {
                ViewName = "ApiError",
                StatusCode = ex.StatusCode,
                ViewData = new ViewDataDictionary(metadataProvider, context.ModelState) { Model = ex.Message }
            };
        }

        context.ExceptionHandled = true;
    }
}
