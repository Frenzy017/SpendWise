using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SpendWise.Data;

/// <summary>Lets "dotnet ef" create the context without starting the API.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SpendWiseDbContext>
{
    public SpendWiseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SpendWiseDbContext>()
            .UseSqlite("Data Source=spendwise.db")
            .Options;
        return new SpendWiseDbContext(options);
    }
}
