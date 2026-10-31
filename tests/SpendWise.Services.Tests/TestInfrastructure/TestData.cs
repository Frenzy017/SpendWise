using SpendWise.Contracts;
using SpendWise.Data;
using SpendWise.Data.Entities;

namespace SpendWise.Services.Tests.TestInfrastructure;

/// <summary>Helpers that insert entities directly, so tests only exercise the service under test.</summary>
public static class TestData
{
    public static User AddUser(this SpendWiseDbContext db, string username, string role = Roles.User, bool isActive = true)
    {
        var user = new User
        {
            Username = username,
            NormalizedUsername = username.ToUpperInvariant(),
            Email = $"{username}@test.com",
            NormalizedEmail = $"{username}@test.com".ToUpperInvariant(),
            PasswordHash = "not-used",
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Category AddCategory(this SpendWiseDbContext db, string name, int? userId = null)
    {
        var category = new Category { Name = name, Color = "#112233", UserId = userId, CreatedAt = DateTime.UtcNow };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Expense AddExpense(this SpendWiseDbContext db, int userId, int categoryId, decimal amount, DateOnly date)
    {
        var expense = new Expense
        {
            UserId = userId,
            CategoryId = categoryId,
            Amount = amount,
            Date = date,
            CreatedAt = DateTime.UtcNow
        };
        db.Expenses.Add(expense);
        db.SaveChanges();
        return expense;
    }

    public static Budget AddBudget(this SpendWiseDbContext db, int userId, int categoryId, int year, int month, decimal limit)
    {
        var budget = new Budget { UserId = userId, CategoryId = categoryId, Year = year, Month = month, Limit = limit };
        db.Budgets.Add(budget);
        db.SaveChanges();
        return budget;
    }
}
