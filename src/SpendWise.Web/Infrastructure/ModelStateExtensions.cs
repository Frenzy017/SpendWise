using Microsoft.AspNetCore.Mvc.ModelBinding;
using SpendWise.Web.ApiClient;

namespace SpendWise.Web.Infrastructure;

public static class ModelStateExtensions
{
    /// <summary>Shows API validation errors next to their fields, or business errors in the summary.</summary>
    public static void AddApiErrors(this ModelStateDictionary modelState, ApiException ex)
    {
        if (ex.Errors.Count == 0)
        {
            modelState.AddModelError(string.Empty, ex.Message);
            return;
        }

        foreach (var (field, messages) in ex.Errors)
            foreach (var message in messages)
                modelState.AddModelError(field, message);
    }
}
