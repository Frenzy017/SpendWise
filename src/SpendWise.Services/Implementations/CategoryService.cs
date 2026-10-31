using Microsoft.EntityFrameworkCore;
using SpendWise.Contracts.Categories;
using SpendWise.Data;
using SpendWise.Data.Entities;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Interfaces;
using SpendWise.Services.Internal;
using SpendWise.Services.Mapping;

namespace SpendWise.Services.Implementations;

public class CategoryService(SpendWiseDbContext db, TimeProvider timeProvider) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetVisibleAsync(int userId)
    {
        var categories = await db.Categories.VisibleTo(userId).ToListAsync();

        // Sorted in memory: SQLite's default collation is case-sensitive.
        return categories
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.ToDto())
            .ToList();
    }

    public async Task<CategoryDto> CreateAsync(int userId, bool isAdmin, CategoryRequest request)
    {
        int? ownerId = isAdmin ? null : userId;
        var name = request.Name.Trim();

        await EnsureNameIsAvailableAsync(ownerId, name, excludeId: null);

        var category = new Category
        {
            Name = name,
            Color = request.Color.ToUpperInvariant(),
            UserId = ownerId,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(int userId, bool isAdmin, int categoryId, CategoryRequest request)
    {
        var category = await GetEditableAsync(userId, isAdmin, categoryId);
        var name = request.Name.Trim();

        await EnsureNameIsAvailableAsync(category.UserId, name, excludeId: category.Id);

        category.Name = name;
        category.Color = request.Color.ToUpperInvariant();
        await db.SaveChangesAsync();

        return category.ToDto();
    }

    public async Task DeleteAsync(int userId, bool isAdmin, int categoryId)
    {
        var category = await GetEditableAsync(userId, isAdmin, categoryId);

        // Checked across all users, so deleting a global category can't orphan anyone's data (FR-CAT-05).
        var inUse = await db.Expenses.AnyAsync(e => e.CategoryId == categoryId)
                    || await db.Budgets.AnyAsync(b => b.CategoryId == categoryId);
        if (inUse)
            throw new ConflictException("Category is in use and cannot be deleted.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Loads a category the caller may change: their own personal category, or a global one if they are an admin.
    /// Other users' categories are reported as not found so their existence isn't revealed.
    /// </summary>
    private async Task<Category> GetEditableAsync(int userId, bool isAdmin, int categoryId)
    {
        var category = await db.Categories.FindAsync(categoryId);
        if (category is null || (category.UserId is not null && category.UserId != userId))
            throw new NotFoundException("Category not found.");

        if (category.IsGlobal && !isAdmin)
            throw new ForbiddenException("Global categories can only be changed by an administrator.");

        return category;
    }

    /// <summary>
    /// FR-CAT-03: a personal name must not match a global category or another of the owner's own;
    /// a global name must not match another global category. Case is ignored.
    /// </summary>
    private async Task EnsureNameIsAvailableAsync(int? ownerId, string name, int? excludeId)
    {
        var namesInScope = await db.Categories
            .Where(c => c.Id != excludeId && (c.UserId == null || c.UserId == ownerId))
            .Select(c => c.Name)
            .ToListAsync();

        if (namesInScope.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException("A category with this name already exists.");
    }
}
