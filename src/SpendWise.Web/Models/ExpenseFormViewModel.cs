using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using SpendWise.Contracts;
using SpendWise.Contracts.Expenses;

namespace SpendWise.Web.Models;

public class ExpenseFormViewModel
{
    public int? Id { get; set; }

    [Required, Range(ValidationLimits.MinAmount, ValidationLimits.MaxAmount, ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
    public decimal? Amount { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Please choose a category."), Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [StringLength(ValidationLimits.DescriptionMaxLength)]
    public string? Description { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Categories { get; set; } = [];

    public ExpenseRequest ToRequest() => new(Amount!.Value, Date, CategoryId!.Value, Description);
}
