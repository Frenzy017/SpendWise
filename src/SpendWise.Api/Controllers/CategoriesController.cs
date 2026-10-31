using Microsoft.AspNetCore.Mvc;
using SpendWise.Api.Infrastructure;
using SpendWise.Contracts.Categories;
using SpendWise.Services.Interfaces;

namespace SpendWise.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>Global categories plus the caller's own, sorted by name (FR-CAT-01).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll() =>
        Ok(await categoryService.GetVisibleAsync(User.GetUserId()));

    /// <summary>Creates a personal category, or a global one when the caller is an Admin (FR-CAT-02, 03, 06).</summary>
    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request)
    {
        var category = await categoryService.CreateAsync(User.GetUserId(), User.IsAdmin(), request);
        return CreatedAtAction(nameof(GetAll), null, category);
    }

    /// <summary>Renames or recolours a category the caller may edit (FR-CAT-03, 04, 06).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request) =>
        await categoryService.UpdateAsync(User.GetUserId(), User.IsAdmin(), id, request);

    /// <summary>Deletes a category that no expense or budget uses (FR-CAT-04, 05, 06).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        await categoryService.DeleteAsync(User.GetUserId(), User.IsAdmin(), id);
        return NoContent();
    }
}
