using Microsoft.EntityFrameworkCore;
using SpendWise.Contracts;
using SpendWise.Contracts.Common;
using SpendWise.Contracts.Expenses;
using SpendWise.Data;
using SpendWise.Data.Entities;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Interfaces;
using SpendWise.Services.Internal;
using SpendWise.Services.Mapping;

namespace SpendWise.Services.Implementations;

public class ExpenseService(SpendWiseDbContext db, TimeProvider timeProvider) : IExpenseService
{
    public async Task<PagedResult<ExpenseDto>> GetPagedAsync(int userId, ExpenseQuery query)
    {
        var today = timeProvider.Today();
        var year = query.Year ?? today.Year;
        var month = query.Month ?? today.Month;
        Guard.ValidMonth(year, month);

        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > ValidationLimits.MaxPageSize)
            throw new BusinessRuleException($"Page must be at least 1 and page size between 1 and {ValidationLimits.MaxPageSize}.");

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);

        var expenses = db.Expenses
            .Where(e => e.UserId == userId && e.Date >= from && e.Date < to);
        if (query.CategoryId is not null)
            expenses = expenses.Where(e => e.CategoryId == query.CategoryId);

        var totalCount = await expenses.CountAsync();
        var items = await expenses
            .Include(e => e.Category)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<ExpenseDto>(
            items.Select(e => e.ToDto()).ToList(), query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<ExpenseDto> GetByIdAsync(int userId, int expenseId) =>
        (await GetOwnAsync(userId, expenseId)).ToDto();

    public async Task<ExpenseDto> CreateAsync(int userId, ExpenseRequest request)
    {
        var category = await ValidateAsync(userId, request);

        var expense = new Expense
        {
            UserId = userId,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        Apply(expense, request, category);

        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense.ToDto();
    }

    public async Task<ExpenseDto> UpdateAsync(int userId, int expenseId, ExpenseRequest request)
    {
        var expense = await GetOwnAsync(userId, expenseId);
        var category = await ValidateAsync(userId, request);

        Apply(expense, request, category);
        await db.SaveChangesAsync();
        return expense.ToDto();
    }

    public async Task DeleteAsync(int userId, int expenseId)
    {
        var expense = await GetOwnAsync(userId, expenseId);
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();
    }

    // Other users' expenses are reported as not found (FR-EXP-06).
    private async Task<Expense> GetOwnAsync(int userId, int expenseId) =>
        await db.Expenses
            .Include(e => e.Category)
            .SingleOrDefaultAsync(e => e.Id == expenseId && e.UserId == userId)
        ?? throw new NotFoundException("Expense not found.");

    /// <summary>Checks FR-EXP-01/02 and returns the expense's category.</summary>
    private async Task<Category> ValidateAsync(int userId, ExpenseRequest request)
    {
        Guard.ValidAmount(request.Amount, "Amount");

        if (request.Date > timeProvider.Today())
            throw new BusinessRuleException("The date cannot be in the future.");

        return await db.Categories.VisibleTo(userId).SingleOrDefaultAsync(c => c.Id == request.CategoryId)
            ?? throw new BusinessRuleException("The selected category does not exist.");
    }

    private static void Apply(Expense expense, ExpenseRequest request, Category category)
    {
        expense.Amount = request.Amount;
        expense.Date = request.Date;
        expense.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        expense.CategoryId = category.Id;
        expense.Category = category;
    }
}
