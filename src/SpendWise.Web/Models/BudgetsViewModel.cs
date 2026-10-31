using SpendWise.Contracts.Budgets;
using SpendWise.Contracts.Categories;

namespace SpendWise.Web.Models;

public record BudgetsViewModel(
    int Year,
    int Month,
    IReadOnlyList<BudgetDto> Budgets,
    IReadOnlyList<CategoryDto> Categories,
    BudgetFormViewModel Form);
