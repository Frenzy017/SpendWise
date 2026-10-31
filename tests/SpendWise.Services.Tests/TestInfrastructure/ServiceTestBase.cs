using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SpendWise.Data;

namespace SpendWise.Services.Tests.TestInfrastructure;

/// <summary>
/// Gives every test its own in-memory SQLite database (real constraints, unlike EF InMemory)
/// and a clock fixed to 2026-10-15.
/// </summary>
public abstract class ServiceTestBase : IDisposable
{
    protected static readonly DateOnly Today = new(2026, 10, 15);

    private readonly SqliteConnection _connection;
    protected readonly SpendWiseDbContext Db;
    protected readonly FakeTimeProvider Clock;

    protected ServiceTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SpendWiseDbContext>()
            .UseSqlite(_connection)
            .Options;
        Db = new SpendWiseDbContext(options);
        Db.Database.EnsureCreated();

        Clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
