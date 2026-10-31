using Microsoft.AspNetCore.Mvc;
using SpendWise.Api.Infrastructure;
using SpendWise.Contracts.Budgets;
using SpendWise.Services.Interfaces;

namespace SpendWise.Api.Controllers;

[ApiController]
[Route("api/budgets")]
[Produces("application/json")]
public class BudgetsController(IBudgetService budgetService, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>The caller's budgets for a month (default: current) (FR-BUD-02).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> GetForMonth(int? year, int? month)
    {
        var (y, m) = timeProvider.Resolve(year, month);
        return Ok(await budgetService.GetForMonthAsync(User.GetUserId(), y, m));
    }

    /// <summary>Creates or updates the budget for a category and month (FR-BUD-01).</summary>
    [HttpPut]
    [ProducesResponseType<BudgetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BudgetDto>> Set(SetBudgetRequest request) =>
        await budgetService.SetAsync(User.GetUserId(), request);

    /// <summary>Deletes one of the caller's budgets (FR-BUD-02).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await budgetService.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    /// <summary>Copies the previous month's budgets into the given month (FR-BUD-03).</summary>
    [HttpPost("copy-previous")]
    [ProducesResponseType<CopyBudgetsResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CopyBudgetsResult>> CopyPrevious(CopyBudgetsRequest request) =>
        await budgetService.CopyFromPreviousMonthAsync(User.GetUserId(), request.Year, request.Month);
}
