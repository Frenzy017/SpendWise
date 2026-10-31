using SpendWise.Contracts.Categories;

namespace SpendWise.Services.Interfaces;

public interface ICategoryService
{
    /// <summary>Global categories plus the user's own, sorted by name.</summary>
    Task<IReadOnlyList<CategoryDto>> GetVisibleAsync(int userId);

    /// <summary>Admins create global categories; users create personal ones.</summary>
    Task<CategoryDto> CreateAsync(int userId, bool isAdmin, CategoryRequest request);

    Task<CategoryDto> UpdateAsync(int userId, bool isAdmin, int categoryId, CategoryRequest request);

    Task DeleteAsync(int userId, bool isAdmin, int categoryId);
}
