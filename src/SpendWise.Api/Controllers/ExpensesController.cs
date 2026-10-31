using Microsoft.AspNetCore.Mvc;
using SpendWise.Api.Infrastructure;
using SpendWise.Contracts.Common;
using SpendWise.Contracts.Expenses;
using SpendWise.Services.Interfaces;

namespace SpendWise.Api.Controllers;

[ApiController]
[Route("api/expenses")]
[Produces("application/json")]
public class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    /// <summary>The caller's expenses for a month (default: current), newest first, paginated (FR-EXP-03).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> GetPaged([FromQuery] ExpenseQuery query) =>
        await expenseService.GetPagedAsync(User.GetUserId(), query);

    /// <summary>One of the caller's expenses. Other users' IDs return 404 (FR-EXP-06).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ExpenseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseDto>> GetById(int id) =>
        await expenseService.GetByIdAsync(User.GetUserId(), id);

    /// <summary>Records an expense (FR-EXP-01, 02).</summary>
    [HttpPost]
    [ProducesResponseType<ExpenseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseDto>> Create(ExpenseRequest request)
    {
        var expense = await expenseService.CreateAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
    }

    /// <summary>Updates one of the caller's expenses (FR-EXP-04).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ExpenseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseDto>> Update(int id, ExpenseRequest request) =>
        await expenseService.UpdateAsync(User.GetUserId(), id, request);

    /// <summary>Deletes one of the caller's expenses (FR-EXP-05).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await expenseService.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
