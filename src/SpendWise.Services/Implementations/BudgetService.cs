using Microsoft.EntityFrameworkCore;
using SpendWise.Contracts.Budgets;
using SpendWise.Data;
using SpendWise.Data.Entities;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Interfaces;
using SpendWise.Services.Internal;
using SpendWise.Services.Mapping;

namespace SpendWise.Services.Implementations;

public class BudgetService(SpendWiseDbContext db) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetDto>> GetForMonthAsync(int userId, int year, int month)
    {
        Guard.ValidMonth(year, month);

        var budgets = await db.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToListAsync();

        return budgets
            .OrderBy(b => b.Category!.Name, StringComparer.OrdinalIgnoreCase)
            .Select(b => b.ToDto())
            .ToList();
    }

    public async Task<BudgetDto> SetAsync(int userId, SetBudgetRequest request)
    {
        Guard.ValidMonth(request.Year, request.Month);
        Guard.ValidAmount(request.Limit, "Limit");

        var category = await db.Categories.VisibleTo(userId).SingleOrDefaultAsync(c => c.Id == request.CategoryId)
            ?? throw new BusinessRuleException("The selected category does not exist.");

        // Upsert: one budget per user, category and month (FR-BUD-01).
        var budget = await db.Budgets.SingleOrDefaultAsync(b =>
            b.UserId == userId && b.CategoryId == request.CategoryId &&
            b.Year == request.Year && b.Month == request.Month);

        if (budget is null)
        {
            budget = new Budget
            {
                UserId = userId,
                CategoryId = category.Id,
                Year = request.Year,
                Month = request.Month
            };
            db.Budgets.Add(budget);
        }

        budget.Limit = request.Limit;
        budget.Category = category;
        await db.SaveChangesAsync();

        return budget.ToDto();
    }

    public async Task DeleteAsync(int userId, int budgetId)
    {
        var budget = await db.Budgets.SingleOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId)
            ?? throw new NotFoundException("Budget not found.");

        db.Budgets.Remove(budget);
        await db.SaveChangesAsync();
    }

    public async Task<CopyBudgetsResult> CopyFromPreviousMonthAsync(int userId, int year, int month)
    {
        Guard.ValidMonth(year, month);
        var previous = new DateOnly(year, month, 1).AddMonths(-1);

        var source = await db.Budgets
            .Where(b => b.UserId == userId && b.Year == previous.Year && b.Month == previous.Month)
            .ToListAsync();

        var existingCategoryIds = (await db.Budgets
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Select(b => b.CategoryId)
            .ToListAsync()).ToHashSet();

        var toCopy = source.Where(b => !existingCategoryIds.Contains(b.CategoryId)).ToList();
        db.Budgets.AddRange(toCopy.Select(b => new Budget
        {
            UserId = userId,
            CategoryId = b.CategoryId,
            Year = year,
            Month = month,
            Limit = b.Limit
        }));
        await db.SaveChangesAsync();

        return new CopyBudgetsResult(toCopy.Count, source.Count - toCopy.Count);
    }
}
