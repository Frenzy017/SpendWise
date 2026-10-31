using SpendWise.Services.Exceptions;
using SpendWise.Services.Implementations;
using SpendWise.Services.Tests.TestInfrastructure;

namespace SpendWise.Services.Tests;

public class ReportServiceTests : ServiceTestBase
{
    private readonly ReportService _sut;
    private readonly int _maria;
    private readonly int _ivan;
    private readonly int _food;
    private readonly int _fun;
    private readonly int _gifts;

    public ReportServiceTests()
    {
        _sut = new ReportService(Db);
        _maria = Db.AddUser("maria").Id;
        _ivan = Db.AddUser("ivan").Id;
        _food = Db.AddCategory("Food").Id;
        _fun = Db.AddCategory("Going out").Id;
        _gifts = Db.AddCategory("Gifts").Id;
    }

    private static DateOnly Oct(int day) => new(2026, 10, day);

    [Fact]
    public async Task GetMonthlySummaryAsync_ComputesTotalCountAndPercentages()
    {
        Db.AddExpense(_maria, _food, 60m, Oct(1));
        Db.AddExpense(_maria, _food, 15m, Oct(2));
        Db.AddExpense(_maria, _fun, 25m, Oct(3));

        var result = await _sut.GetMonthlySummaryAsync(_maria, 2026, 10);

        Assert.Equal(100m, result.Total);
        Assert.Equal(3, result.ExpenseCount);
        Assert.Equal(75m, result.Categories.Single(c => c.CategoryId == _food).PercentOfTotal);
        Assert.Equal(25m, result.Categories.Single(c => c.CategoryId == _fun).PercentOfTotal);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_PercentagesRoundedToOneDecimal()
    {
        Db.AddExpense(_maria, _food, 1m, Oct(1));
        Db.AddExpense(_maria, _fun, 2m, Oct(1));

        var result = await _sut.GetMonthlySummaryAsync(_maria, 2026, 10);

        Assert.Equal(33.3m, result.Categories.Single(c => c.CategoryId == _food).PercentOfTotal);
        Assert.Equal(66.7m, result.Categories.Single(c => c.CategoryId == _fun).PercentOfTotal);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ComparesWithPreviousMonth()
    {
        Db.AddExpense(_maria, _food, 200m, new DateOnly(2026, 9, 10));
        Db.AddExpense(_maria, _food, 150m, Oct(10));

        var result = await _sut.GetMonthlySummaryAsync(_maria, 2026, 10);

        Assert.Equal(200m, result.PreviousMonthTotal);
        Assert.Equal(-25m, result.ChangePercent);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_NoSpendingPreviousMonth_ChangePercentIsNull()
    {
        Db.AddExpense(_maria, _food, 150m, Oct(10));

        var result = await _sut.GetMonthlySummaryAsync(_maria, 2026, 10);

        Assert.Equal(0m, result.PreviousMonthTotal);
        Assert.Null(result.ChangePercent);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_SpentOverLimit_IsOverBudgetWithNegativeRemaining()
    {
        Db.AddBudget(_maria, _fun, 2026, 10, 100m);
        Db.AddExpense(_maria, _fun, 130m, Oct(5));

        var line = (await _sut.GetMonthlySummaryAsync(_maria, 2026, 10)).Categories.Single();

        Assert.True(line.IsOverBudget);
        Assert.Equal(100m, line.BudgetLimit);
        Assert.Equal(-30m, line.Remaining);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_SpentEqualsLimit_IsNotOverBudget()
    {
        Db.AddBudget(_maria, _fun, 2026, 10, 100m);
        Db.AddExpense(_maria, _fun, 100m, Oct(5));

        var line = (await _sut.GetMonthlySummaryAsync(_maria, 2026, 10)).Categories.Single();

        Assert.False(line.IsOverBudget);
        Assert.Equal(0m, line.Remaining);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_NoBudget_NeverOverBudget()
    {
        Db.AddExpense(_maria, _food, 5000m, Oct(5));

        var line = (await _sut.GetMonthlySummaryAsync(_maria, 2026, 10)).Categories.Single();

        Assert.False(line.IsOverBudget);
        Assert.Null(line.BudgetLimit);
        Assert.Null(line.Remaining);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_BudgetWithoutSpending_AppearsWithZero()
    {
        Db.AddBudget(_maria, _gifts, 2026, 10, 50m);

        var line = (await _sut.GetMonthlySummaryAsync(_maria, 2026, 10)).Categories.Single();

        Assert.Equal(_gifts, line.CategoryId);
        Assert.Equal(0m, line.Spent);
        Assert.Equal(50m, line.Remaining);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_EmptyMonth_ReturnsZeros()
    {
        var result = await _sut.GetMonthlySummaryAsync(_maria, 2026, 10);

        Assert.Equal(0m, result.Total);
        Assert.Equal(0, result.ExpenseCount);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_SortsCategoriesBySpentDescending()
    {
        Db.AddExpense(_maria, _food, 10m, Oct(1));
        Db.AddExpense(_maria, _fun, 50m, Oct(1));
        Db.AddExpense(_maria, _gifts, 30m, Oct(1));

        var result = await _sut.GetMonthlySummaryAsync(_maria, 2026, 10);

        Assert.Equal([_fun, _gifts, _food], result.Categories.Select(c => c.CategoryId));
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_IgnoresOtherUsersData()
    {
        Db.AddExpense(_ivan, _food, 999m, Oct(1));
        Db.AddBudget(_ivan, _food, 2026, 10, 10m);

        var result = await _sut.GetMonthlySummaryAsync(_maria, 2026, 10);

        Assert.Equal(0m, result.Total);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetTrendAsync_SpansYearBoundaryAndZeroFillsMissingMonths()
    {
        Db.AddExpense(_maria, _food, 10m, new DateOnly(2025, 11, 3));
        Db.AddExpense(_maria, _food, 20m, new DateOnly(2026, 1, 3));
        Db.AddExpense(_maria, _food, 5m, new DateOnly(2026, 1, 20));
        Db.AddExpense(_maria, _food, 99m, new DateOnly(2025, 10, 31)); // just outside the range

        var result = await _sut.GetTrendAsync(_maria, 2026, 2, 4);

        Assert.Equal(
            [(2025, 11, 10m), (2025, 12, 0m), (2026, 1, 25m), (2026, 2, 0m)],
            result.Select(t => (t.Year, t.Month, t.Total)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GetTrendAsync_MonthsOutOfRange_ThrowsBusinessRule(int months)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetTrendAsync(_maria, 2026, 10, months));
    }
}
