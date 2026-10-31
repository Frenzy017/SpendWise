using SpendWise.Contracts.Auth;
using SpendWise.Contracts.Budgets;
using SpendWise.Contracts.Categories;
using SpendWise.Contracts.Expenses;
using SpendWise.Data.Entities;

namespace SpendWise.Services.Mapping;

/// <summary>Entity to DTO mapping. Entities never leave the Services layer.</summary>
public static class MappingExtensions
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Username, user.Email, user.Role);

    public static CategoryDto ToDto(this Category category) =>
        new(category.Id, category.Name, category.Color, category.IsGlobal);

    // Expense and Budget mappings require the Category navigation to be loaded.
    public static ExpenseDto ToDto(this Expense expense) =>
        new(expense.Id, expense.Amount, expense.Date, expense.Description,
            expense.CategoryId, expense.Category!.Name, expense.Category.Color);

    public static BudgetDto ToDto(this Budget budget) =>
        new(budget.Id, budget.CategoryId, budget.Category!.Name, budget.Category.Color,
            budget.Year, budget.Month, budget.Limit);
}
