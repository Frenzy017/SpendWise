using System.ComponentModel.DataAnnotations;

namespace SpendWise.Contracts.Expenses;

/// <summary>Query-string filter for listing expenses. Year and month default to the current month.</summary>
public record ExpenseQuery
{
    [Range(ValidationLimits.MinYear, ValidationLimits.MaxYear)]
    public int? Year { get; init; }

    [Range(1, 12)]
    public int? Month { get; init; }

    public int? CategoryId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, ValidationLimits.MaxPageSize)]
    public int PageSize { get; init; } = ValidationLimits.DefaultPageSize;
}
