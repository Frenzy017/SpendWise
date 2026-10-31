namespace SpendWise.Contracts.Budgets;

public record BudgetDto(
    int Id,
    int CategoryId,
    string CategoryName,
    string CategoryColor,
    int Year,
    int Month,
    decimal Limit);
