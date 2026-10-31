using SpendWise.Data.Entities;

namespace SpendWise.Services.Internal;

internal static class CategoryQueries
{
    /// <summary>Global categories plus the user's own personal ones (FR-CAT-01).</summary>
    public static IQueryable<Category> VisibleTo(this IQueryable<Category> categories, int userId) =>
        categories.Where(c => c.UserId == null || c.UserId == userId);
}
