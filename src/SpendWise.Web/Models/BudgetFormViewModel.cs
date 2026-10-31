using System.ComponentModel.DataAnnotations;
using SpendWise.Contracts;
using SpendWise.Contracts.Budgets;

namespace SpendWise.Web.Models;

public class BudgetFormViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }

    [Required(ErrorMessage = "Please choose a category."), Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required, Range(ValidationLimits.MinAmount, ValidationLimits.MaxAmount, ErrorMessage = "Limit must be between 0.01 and 1,000,000.")]
    public decimal? Limit { get; set; }

    public SetBudgetRequest ToRequest() => new(CategoryId!.Value, Year, Month, Limit!.Value);
}
