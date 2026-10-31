using SpendWise.Contracts.Categories;
using SpendWise.Contracts.Common;
using SpendWise.Contracts.Expenses;

namespace SpendWise.Web.Models;

public record ExpenseListViewModel(
    int Year,
    int Month,
    int? CategoryId,
    PagedResult<ExpenseDto> Expenses,
    IReadOnlyList<CategoryDto> Categories);
