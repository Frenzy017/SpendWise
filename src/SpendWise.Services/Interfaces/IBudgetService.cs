using SpendWise.Contracts.Budgets;

namespace SpendWise.Services.Interfaces;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetDto>> GetForMonthAsync(int userId, int year, int month);

    /// <summary>Creates the budget, or updates the limit if one exists for that category and month.</summary>
    Task<BudgetDto> SetAsync(int userId, SetBudgetRequest request);

    Task DeleteAsync(int userId, int budgetId);

    /// <summary>Copies last month's budgets into the given month, skipping categories that already have one.</summary>
    Task<CopyBudgetsResult> CopyFromPreviousMonthAsync(int userId, int year, int month);
}
