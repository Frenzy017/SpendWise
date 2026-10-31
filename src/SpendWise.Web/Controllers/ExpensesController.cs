using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SpendWise.Web.ApiClient;
using SpendWise.Web.Infrastructure;
using SpendWise.Web.Models;

namespace SpendWise.Web.Controllers;

[Authorize]
public class ExpensesController(SpendWiseApiClient api) : Controller
{
    /// <summary>The month's expenses, optionally for one category, 20 per page (US-07).</summary>
    public async Task<IActionResult> Index(int? year, int? month, int? categoryId, int page = 1)
    {
        var (y, m) = MonthHelper.Resolve(year, month);
        var expenses = await api.GetExpensesAsync(y, m, categoryId, Math.Max(page, 1));
        var categories = await api.GetCategoriesAsync();
        return View(new ExpenseListViewModel(y, m, categoryId, expenses, categories));
    }

    [HttpGet]
    public async Task<IActionResult> Create() =>
        View("Form", await WithCategoriesAsync(new ExpenseFormViewModel { Date = DateOnly.FromDateTime(DateTime.Today) }));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await api.CreateExpenseAsync(model.ToRequest());
                TempData["Success"] = "Expense added.";
                return RedirectToAction(nameof(Index), new { year = model.Date.Year, month = model.Date.Month });
            }
            catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status400BadRequest)
            {
                ModelState.AddApiErrors(ex);
            }
        }
        return View("Form", await WithCategoriesAsync(model));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var expense = await api.GetExpenseAsync(id);
        return View("Form", await WithCategoriesAsync(new ExpenseFormViewModel
        {
            Id = expense.Id,
            Amount = expense.Amount,
            Date = expense.Date,
            CategoryId = expense.CategoryId,
            Description = expense.Description
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExpenseFormViewModel model)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await api.UpdateExpenseAsync(id, model.ToRequest());
                TempData["Success"] = "Expense updated.";
                return RedirectToAction(nameof(Index), new { year = model.Date.Year, month = model.Date.Month });
            }
            catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status400BadRequest)
            {
                ModelState.AddApiErrors(ex);
            }
        }
        return View("Form", await WithCategoriesAsync(model));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int year, int month)
    {
        await api.DeleteExpenseAsync(id);
        TempData["Success"] = "Expense deleted.";
        return RedirectToAction(nameof(Index), new { year, month });
    }

    private async Task<ExpenseFormViewModel> WithCategoriesAsync(ExpenseFormViewModel model)
    {
        var categories = await api.GetCategoriesAsync();
        model.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));
        return model;
    }
}
