using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendWise.Contracts.Budgets;
using SpendWise.Web.ApiClient;
using SpendWise.Web.Infrastructure;
using SpendWise.Web.Models;

namespace SpendWise.Web.Controllers;

[Authorize]
public class BudgetsController(SpendWiseApiClient api) : Controller
{
    /// <summary>The month's budgets with a form to set one (US-09).</summary>
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var (y, m) = MonthHelper.Resolve(year, month);
        var budgets = await api.GetBudgetsAsync(y, m);
        var categories = await api.GetCategoriesAsync();
        return View(new BudgetsViewModel(y, m, budgets, categories, new BudgetFormViewModel { Year = y, Month = m }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Set([Bind(Prefix = "Form")] BudgetFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
        }
        else
        {
            try
            {
                var budget = await api.SetBudgetAsync(model.ToRequest());
                TempData["Success"] = $"Budget for {budget.CategoryName} set to {Format.Money(budget.Limit)}.";
            }
            catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status400BadRequest)
            {
                TempData["Error"] = ex.Message;
            }
        }
        return RedirectToAction(nameof(Index), new { year = model.Year, month = model.Month });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int year, int month)
    {
        await api.DeleteBudgetAsync(id);
        TempData["Success"] = "Budget removed.";
        return RedirectToAction(nameof(Index), new { year, month });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyPrevious(int year, int month)
    {
        var result = await api.CopyBudgetsAsync(new CopyBudgetsRequest(year, month));
        TempData["Success"] = result.CopiedCount == 0
            ? "Nothing to copy from last month."
            : $"Copied {result.CopiedCount} budget(s) from last month" +
              (result.SkippedCount > 0 ? $", skipped {result.SkippedCount} that already existed." : ".");
        return RedirectToAction(nameof(Index), new { year, month });
    }
}
