using Microsoft.EntityFrameworkCore;
using SpendWise.Data.Entities;

namespace SpendWise.Data.Seeding;

/// <summary>
/// Fills an empty database with demo users, global categories, expenses and budgets (plan §3).
/// Dates are relative to "today", so the dashboard always has fresh data.
/// </summary>
public static class DbSeeder
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

    /// <param name="hashPassword">Password hashing is a Services concern, so the caller supplies it.</param>
    public static async Task SeedAsync(SpendWiseDbContext db, Func<User, string, string> hashPassword, DateOnly today)
    {
        if (await db.Users.AnyAsync())
            return; // only seed an empty database

        var now = DateTime.UtcNow;

        // ---- Users ----
        User NewUser(string username, string password, string role, bool isActive = true)
        {
            var user = new User
            {
                Username = username,
                NormalizedUsername = username.ToUpperInvariant(),
                Email = $"{username}@spendwise.local",
                NormalizedEmail = $"{username}@spendwise.local".ToUpperInvariant(),
                Role = role,
                IsActive = isActive,
                CreatedAt = now.AddDays(-200)
            };
            user.PasswordHash = hashPassword(user, password);
            return user;
        }

        var admin = NewUser("admin", "Admin123!", AdminRole);
        var maria = NewUser("maria", "Maria123!", UserRole);
        var ivan = NewUser("ivan", "Ivan1234!", UserRole);
        var blocked = NewUser("blocked", "Blocked123!", UserRole, isActive: false);
        db.Users.AddRange(admin, maria, ivan, blocked);

        // ---- Global categories ----
        Category Global(string name, string color) => new() { Name = name, Color = color, CreatedAt = now };
        var food = Global("Food", "#198754");
        var clothes = Global("Clothes", "#6F42C1");
        var goingOut = Global("Going out", "#FD7E14");
        var subscriptions = Global("Subscriptions", "#0D6EFD");
        var gifts = Global("Gifts", "#D63384");
        var transport = Global("Transport", "#20C997");
        var health = Global("Health", "#DC3545");
        var other = Global("Other", "#6C757D");
        db.Categories.AddRange(food, clothes, goingOut, subscriptions, gifts, transport, health, other);

        // Maria's personal category
        var gym = new Category { Name = "Gym", Color = "#0DCAF0", User = maria, CreatedAt = now };
        db.Categories.Add(gym);

        // ---- Maria: about 40 expenses over the current and 5 previous months ----
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var random = new Random(42); // fixed seed, so the demo data is the same every time

        for (var monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
        {
            var month = currentMonth.AddMonths(-monthsAgo);

            void Add(Category category, decimal amount, int day, string description)
            {
                // Previous months vary by up to ±20%; the current month is fixed so the budget demo is predictable.
                var factor = monthsAgo == 0 ? 1m : 0.8m + (decimal)random.NextDouble() * 0.4m;
                db.Expenses.Add(new Expense
                {
                    User = maria,
                    Category = category,
                    Amount = Math.Round(amount * factor, 2),
                    Date = ClampDay(month, day, today),
                    Description = description,
                    CreatedAt = now
                });
            }

            Add(food, 64.20m, 2, "Weekly shop");
            Add(food, 12.80m, 9, "Lunch at uni");
            Add(food, 48.75m, 16, "Groceries");
            Add(goingOut, 38.00m, 6, "Cinema and dinner");
            Add(goingOut, 54.50m, 13, "Concert tickets");
            Add(subscriptions, 12.99m, 1, "Netflix");
            Add(subscriptions, 10.99m, 1, "Spotify");
            Add(gym, 35.90m, 3, "Monthly pass");
            if (monthsAgo % 2 == 0) Add(clothes, 79.90m, 20, "Winter jacket");
            if (monthsAgo % 3 == 1) Add(gifts, 30.00m, 18, "Birthday present");
            if (monthsAgo == 0) Add(goingOut, 27.40m, 10, "Drinks with friends");
        }

        // Budgets for the current and previous month; "Going out" is over budget this month.
        foreach (var month in new[] { currentMonth, currentMonth.AddMonths(-1) })
        {
            void Budget(Category category, decimal limit) => db.Budgets.Add(new Budget
            {
                User = maria, Category = category, Year = month.Year, Month = month.Month, Limit = limit
            });

            Budget(food, 250m);
            Budget(goingOut, 100m);
            Budget(subscriptions, 30m);
            Budget(clothes, 120m);
            Budget(gifts, 50m);
        }

        // ---- Ivan: a few expenses, to show that users can't see each other's data ----
        foreach (var (category, amount, day, description) in new[]
                 {
                     (food, 20.40m, 2, "Bakery"),
                     (transport, 15.00m, 4, "Bus card top-up"),
                     (goingOut, 40.00m, 7, "Football match"),
                     (health, 18.60m, 11, "Pharmacy")
                 })
        {
            db.Expenses.Add(new Expense
            {
                User = ivan,
                Category = category,
                Amount = amount,
                Date = ClampDay(currentMonth, day, today),
                Description = description,
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>A day in the given month that is valid and not in the future.</summary>
    private static DateOnly ClampDay(DateOnly month, int day, DateOnly today)
    {
        var date = new DateOnly(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)));
        return date > today ? today : date;
    }
}
