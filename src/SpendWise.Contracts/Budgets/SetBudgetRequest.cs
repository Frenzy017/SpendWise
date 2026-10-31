using System.ComponentModel.DataAnnotations;

namespace SpendWise.Contracts.Budgets;

public record SetBudgetRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")] int CategoryId,
    [Range(ValidationLimits.MinYear, ValidationLimits.MaxYear)] int Year,
    [Range(1, 12)] int Month,
    [Range(ValidationLimits.MinAmount, ValidationLimits.MaxAmount, ErrorMessage = "Limit must be between 0.01 and 1,000,000.")]
    decimal Limit);
