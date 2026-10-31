using System.ComponentModel.DataAnnotations;

namespace SpendWise.Contracts.Budgets;

/// <summary>The target month; budgets are copied from the month before it.</summary>
public record CopyBudgetsRequest(
    [Range(ValidationLimits.MinYear, ValidationLimits.MaxYear)] int Year,
    [Range(1, 12)] int Month);
