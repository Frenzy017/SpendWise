using System.ComponentModel.DataAnnotations;

namespace SpendWise.Contracts.Expenses;

/// <summary>Used both to create and to update an expense.</summary>
public record ExpenseRequest(
    [Range(ValidationLimits.MinAmount, ValidationLimits.MaxAmount, ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
    decimal Amount,
    DateOnly Date,
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    int CategoryId,
    [StringLength(ValidationLimits.DescriptionMaxLength)]
    string? Description);
