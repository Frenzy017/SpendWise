using SpendWise.Contracts.Common;
using SpendWise.Contracts.Expenses;

namespace SpendWise.Services.Interfaces;

public interface IExpenseService
{
    Task<PagedResult<ExpenseDto>> GetPagedAsync(int userId, ExpenseQuery query);
    Task<ExpenseDto> GetByIdAsync(int userId, int expenseId);
    Task<ExpenseDto> CreateAsync(int userId, ExpenseRequest request);
    Task<ExpenseDto> UpdateAsync(int userId, int expenseId, ExpenseRequest request);
    Task DeleteAsync(int userId, int expenseId);
}
