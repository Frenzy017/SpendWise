namespace SpendWise.Contracts.Expenses;

public record ExpenseDto(
    int Id,
    decimal Amount,
    DateOnly Date,
    string? Description,
    int CategoryId,
    string CategoryName,
    string CategoryColor);
