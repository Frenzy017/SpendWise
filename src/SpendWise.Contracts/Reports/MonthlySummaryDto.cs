namespace SpendWise.Contracts.Reports;

public record MonthlySummaryDto(
    int Year,
    int Month,
    decimal Total,
    int ExpenseCount,
    decimal PreviousMonthTotal,
    decimal? ChangePercent,
    IReadOnlyList<CategorySummaryDto> Categories);
