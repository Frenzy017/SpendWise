using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendWise.Contracts;
using SpendWise.Web.ApiClient;
using SpendWise.Web.Infrastructure;
using SpendWise.Web.Models;

namespace SpendWise.Web.Controllers;

[Authorize]
public class CategoriesController(SpendWiseApiClient api) : Controller
{
    /// <summary>Global and personal categories, with a form to add one (US-03, US-04, US-05).</summary>
    public async Task<IActionResult> Index() =>
        View(new CategoriesViewModel(await api.GetCategoriesAsync(), User.IsInRole(Roles.Admin), new CategoryFormViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewCategory")] CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError() ?? "Please check the category details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await api.CreateCategoryAsync(model.ToRequest());
            TempData["Success"] = $"Category \"{model.Name}\" added.";
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 409)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = (await api.GetCategoriesAsync()).FirstOrDefault(c => c.Id == id);
        if (category is null)
            return NotFound();

        return View(new CategoryFormViewModel { Id = category.Id, Name = category.Name, Color = category.Color });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await api.UpdateCategoryAsync(id, model.ToRequest());
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 403 or 409)
        {
            ModelState.AddApiErrors(ex);
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await api.DeleteCategoryAsync(id);
            TempData["Success"] = "Category deleted.";
        }
        catch (ApiException ex) when (ex.StatusCode is 403 or 409)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private string? FirstError() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
}
