using Microsoft.EntityFrameworkCore;
using SpendWise.Data.Entities;

namespace SpendWise.Data;

public class SpendWiseDbContext(DbContextOptions<SpendWiseDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SpendWiseDbContext).Assembly);
    }
}
