using SpendWise.Contracts.Budgets;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Implementations;
using SpendWise.Services.Tests.TestInfrastructure;

namespace SpendWise.Services.Tests;

public class BudgetServiceTests : ServiceTestBase
{
    private readonly BudgetService _sut;
    private readonly int _maria;
    private readonly int _ivan;
    private readonly int _food;
    private readonly int _fun;

    public BudgetServiceTests()
    {
        _sut = new BudgetService(Db);
        _maria = Db.AddUser("maria").Id;
        _ivan = Db.AddUser("ivan").Id;
        _food = Db.AddCategory("Food").Id;
        _fun = Db.AddCategory("Going out").Id;
    }

    [Fact]
    public async Task SetAsync_NoExistingBudget_CreatesIt()
    {
        var result = await _sut.SetAsync(_maria, new SetBudgetRequest(_food, 2026, 10, 300m));

        Assert.Equal(300m, result.Limit);
        Assert.Equal("Food", result.CategoryName);
        Assert.Single(Db.Budgets);
    }

    [Fact]
    public async Task SetAsync_ExistingBudget_UpdatesLimitWithoutDuplicate()
    {
        await _sut.SetAsync(_maria, new SetBudgetRequest(_food, 2026, 10, 300m));

        var result = await _sut.SetAsync(_maria, new SetBudgetRequest(_food, 2026, 10, 250m));

        Assert.Equal(250m, result.Limit);
        Assert.Equal(250m, Db.Budgets.Single().Limit);
    }

    [Fact]
    public async Task SetAsync_InvisibleCategory_ThrowsBusinessRule()
    {
        var ivans = Db.AddCategory("Fishing", _ivan);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetAsync(_maria, new SetBudgetRequest(ivans.Id, 2026, 10, 10m)));
    }

    [Theory]
    [InlineData(2026, 0, 10)]
    [InlineData(2026, 13, 10)]
    [InlineData(2026, 10, 0)]
    public async Task SetAsync_InvalidMonthOrLimit_ThrowsBusinessRule(int year, int month, decimal limit)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetAsync(_maria, new SetBudgetRequest(_food, year, month, limit)));
    }

    [Fact]
    public async Task GetForMonthAsync_ReturnsOnlyOwnBudgetsForThatMonth()
    {
        Db.AddBudget(_maria, _food, 2026, 10, 100m);
        Db.AddBudget(_maria, _food, 2026, 9, 100m);
        Db.AddBudget(_ivan, _food, 2026, 10, 100m);

        var result = await _sut.GetForMonthAsync(_maria, 2026, 10);

        Assert.Single(result);
    }

    [Fact]
    public async Task DeleteAsync_OwnBudget_RemovesIt()
    {
        var budget = Db.AddBudget(_maria, _food, 2026, 10, 100m);

        await _sut.DeleteAsync(_maria, budget.Id);

        Assert.Empty(Db.Budgets);
    }

    [Fact]
    public async Task DeleteAsync_AnotherUsersBudget_ThrowsNotFound()
    {
        var budget = Db.AddBudget(_ivan, _food, 2026, 10, 100m);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(_maria, budget.Id));
    }

    [Fact]
    public async Task CopyFromPreviousMonthAsync_CopiesMissingAndSkipsExisting()
    {
        Db.AddBudget(_maria, _food, 2026, 9, 300m);
        Db.AddBudget(_maria, _fun, 2026, 9, 150m);
        Db.AddBudget(_maria, _food, 2026, 10, 280m);

        var result = await _sut.CopyFromPreviousMonthAsync(_maria, 2026, 10);

        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(1, result.SkippedCount);
        var october = await _sut.GetForMonthAsync(_maria, 2026, 10);
        Assert.Equal(280m, october.Single(b => b.CategoryId == _food).Limit);
        Assert.Equal(150m, october.Single(b => b.CategoryId == _fun).Limit);
    }

    [Fact]
    public async Task CopyFromPreviousMonthAsync_January_CopiesFromDecemberOfPreviousYear()
    {
        Db.AddBudget(_maria, _food, 2025, 12, 300m);

        var result = await _sut.CopyFromPreviousMonthAsync(_maria, 2026, 1);

        Assert.Equal(1, result.CopiedCount);
        Assert.Single(await _sut.GetForMonthAsync(_maria, 2026, 1));
    }
}
