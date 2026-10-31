using SpendWise.Contracts.Categories;

namespace SpendWise.Web.Models;

public record CategoriesViewModel(IReadOnlyList<CategoryDto> Categories, bool IsAdmin, CategoryFormViewModel NewCategory);
