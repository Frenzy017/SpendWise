namespace SpendWise.Contracts.Reports;

public record CategorySummaryDto(
    int CategoryId,
    string CategoryName,
    string CategoryColor,
    decimal Spent,
    decimal PercentOfTotal,
    decimal? BudgetLimit,
    decimal? Remaining,
    bool IsOverBudget);
