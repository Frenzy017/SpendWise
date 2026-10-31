using Microsoft.EntityFrameworkCore;
using SpendWise.Contracts;
using SpendWise.Contracts.Reports;
using SpendWise.Data;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Interfaces;
using SpendWise.Services.Internal;

namespace SpendWise.Services.Implementations;

// SQLite can't SUM decimals in the database, so amounts are loaded and added up in memory.
// That's one user's month (or a few months) of rows, which is small.
public class ReportService(SpendWiseDbContext db) : IReportService
{
    public async Task<MonthlySummaryDto> GetMonthlySummaryAsync(int userId, int year, int month)
    {
        Guard.ValidMonth(year, month);
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);
        var previousFrom = from.AddMonths(-1);

        var expenses = await db.Expenses
            .Where(e => e.UserId == userId && e.Date >= from && e.Date < to)
            .Select(e => new { e.CategoryId, e.Amount })
            .ToListAsync();

        var previousAmounts = await db.Expenses
            .Where(e => e.UserId == userId && e.Date >= previousFrom && e.Date < from)
            .Select(e => e.Amount)
            .ToListAsync();

        var budgets = await db.Budgets
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToDictionaryAsync(b => b.CategoryId, b => b.Limit);

        var spentByCategory = expenses
            .GroupBy(e => e.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        // Categories with spending OR a budget appear in the breakdown (FR-REP-02).
        var categoryIds = spentByCategory.Keys.Union(budgets.Keys).ToList();
        var categories = await db.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var total = expenses.Sum(e => e.Amount);
        var previousTotal = previousAmounts.Sum();

        var lines = categoryIds
            .Select(id =>
            {
                var spent = spentByCategory.GetValueOrDefault(id);
                decimal? limit = budgets.TryGetValue(id, out var l) ? l : null;
                return new CategorySummaryDto(
                    id,
                    categories[id].Name,
                    categories[id].Color,
                    spent,
                    Percent(spent, total),
                    limit,
                    limit - spent,
                    limit is not null && spent > limit);
            })
            .OrderByDescending(l => l.Spent)
            .ThenBy(l => l.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        decimal? change = previousTotal == 0
            ? null
            : Math.Round((total - previousTotal) / previousTotal * 100, 1, MidpointRounding.AwayFromZero);

        return new MonthlySummaryDto(year, month, total, expenses.Count, previousTotal, change, lines);
    }

    public async Task<IReadOnlyList<MonthlyTotalDto>> GetTrendAsync(int userId, int year, int month, int months)
    {
        Guard.ValidMonth(year, month);
        if (months < 1 || months > ValidationLimits.MaxTrendMonths)
            throw new BusinessRuleException($"Months must be between 1 and {ValidationLimits.MaxTrendMonths}.");

        var last = new DateOnly(year, month, 1);
        var first = last.AddMonths(-(months - 1));
        var to = last.AddMonths(1);

        var expenses = await db.Expenses
            .Where(e => e.UserId == userId && e.Date >= first && e.Date < to)
            .Select(e => new { e.Date, e.Amount })
            .ToListAsync();

        var totals = expenses
            .GroupBy(e => (e.Date.Year, e.Date.Month))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        // Every month is listed, including those with no spending (FR-REP-03).
        return Enumerable.Range(0, months)
            .Select(i => first.AddMonths(i))
            .Select(d => new MonthlyTotalDto(d.Year, d.Month, totals.GetValueOrDefault((d.Year, d.Month))))
            .ToList();
    }

    private static decimal Percent(decimal part, decimal total) =>
        total == 0 ? 0 : Math.Round(part / total * 100, 1, MidpointRounding.AwayFromZero);
}
