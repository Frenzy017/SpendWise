using System.ComponentModel.DataAnnotations;
using SpendWise.Contracts;
using SpendWise.Contracts.Categories;

namespace SpendWise.Web.Models;

public class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(ValidationLimits.CategoryNameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required, RegularExpression(ValidationLimits.ColorPattern, ErrorMessage = "Pick a colour.")]
    public string Color { get; set; } = "#0D6EFD";

    public CategoryRequest ToRequest() => new(Name, Color);
}
