using System.ComponentModel.DataAnnotations;

namespace SpendWise.Contracts.Categories;

/// <summary>Used both to create and to update a category.</summary>
public record CategoryRequest(
    [Required, StringLength(ValidationLimits.CategoryNameMaxLength, MinimumLength = 1)]
    string Name,
    [Required, RegularExpression(ValidationLimits.ColorPattern, ErrorMessage = "Color must be a hex code like #1A2B3C.")]
    string Color);
