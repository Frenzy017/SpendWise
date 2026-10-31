using SpendWise.Contracts.Expenses;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Implementations;
using SpendWise.Services.Tests.TestInfrastructure;

namespace SpendWise.Services.Tests;

public class ExpenseServiceTests : ServiceTestBase
{
    private readonly ExpenseService _sut;
    private readonly int _maria;
    private readonly int _ivan;
    private readonly int _food;
    private readonly int _clothes;

    public ExpenseServiceTests()
    {
        _sut = new ExpenseService(Db, Clock);
        _maria = Db.AddUser("maria").Id;
        _ivan = Db.AddUser("ivan").Id;
        _food = Db.AddCategory("Food").Id;
        _clothes = Db.AddCategory("Clothes").Id;
    }

    private ExpenseRequest Request(decimal amount = 12.50m, DateOnly? date = null, int? categoryId = null, string? description = "Lunch") =>
        new(amount, date ?? Today, categoryId ?? _food, description);

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesAndReturnsDto()
    {
        var result = await _sut.CreateAsync(_maria, Request());

        Assert.Equal(12.50m, result.Amount);
        Assert.Equal("Food", result.CategoryName);
        Assert.Equal("Lunch", result.Description);
        Assert.Equal(_maria, Db.Expenses.Single().UserId);
    }

    [Fact]
    public async Task CreateAsync_DateToday_Succeeds()
    {
        var result = await _sut.CreateAsync(_maria, Request(date: Today));

        Assert.Equal(Today, result.Date);
    }

    [Fact]
    public async Task CreateAsync_FutureDate_ThrowsBusinessRule()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(_maria, Request(date: Today.AddDays(1))));
        Assert.Empty(Db.Expenses);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1_000_000.01)]
    public async Task CreateAsync_AmountOutOfRange_ThrowsBusinessRule(decimal amount)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(_maria, Request(amount: amount)));
    }

    [Fact]
    public async Task CreateAsync_AnotherUsersCategory_ThrowsBusinessRule()
    {
        var ivans = Db.AddCategory("Fishing", _ivan);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(_maria, Request(categoryId: ivans.Id)));
    }

    [Fact]
    public async Task CreateAsync_BlankDescription_StoredAsNull()
    {
        var result = await _sut.CreateAsync(_maria, Request(description: "   "));

        Assert.Null(result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_AnotherUsersExpense_ThrowsNotFound()
    {
        var ivans = Db.AddExpense(_ivan, _food, 5m, Today);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(_maria, ivans.Id));
    }

    [Fact]
    public async Task GetPagedAsync_NoMonthGiven_ReturnsCurrentMonthNewestFirst()
    {
        Db.AddExpense(_maria, _food, 1m, new DateOnly(2026, 10, 1));
        Db.AddExpense(_maria, _food, 2m, new DateOnly(2026, 10, 14));
        Db.AddExpense(_maria, _food, 3m, new DateOnly(2026, 9, 30));

        var result = await _sut.GetPagedAsync(_maria, new ExpenseQuery());

        Assert.Equal([2m, 1m], result.Items.Select(e => e.Amount));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_CategoryFilter_ReturnsOnlyThatCategory()
    {
        Db.AddExpense(_maria, _food, 1m, Today);
        Db.AddExpense(_maria, _clothes, 2m, Today);

        var result = await _sut.GetPagedAsync(_maria, new ExpenseQuery { CategoryId = _clothes });

        Assert.Equal("Clothes", Assert.Single(result.Items).CategoryName);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsRemainingItemsAndTotals()
    {
        for (var day = 1; day <= 5; day++)
            Db.AddExpense(_maria, _food, day, new DateOnly(2026, 10, day));

        var result = await _sut.GetPagedAsync(_maria, new ExpenseQuery { Page = 2, PageSize = 2 });

        Assert.Equal([3m, 2m], result.Items.Select(e => e.Amount));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetPagedAsync_OtherUsersExpenses_AreNotReturned()
    {
        Db.AddExpense(_ivan, _food, 99m, Today);

        var result = await _sut.GetPagedAsync(_maria, new ExpenseQuery());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetPagedAsync_InvalidMonth_ThrowsBusinessRule()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetPagedAsync(_maria, new ExpenseQuery { Year = 2026, Month = 13 }));
    }

    [Fact]
    public async Task UpdateAsync_OwnExpense_UpdatesFields()
    {
        var expense = Db.AddExpense(_maria, _food, 5m, Today);

        var result = await _sut.UpdateAsync(_maria, expense.Id, Request(amount: 40m, date: Today.AddDays(-3), categoryId: _clothes, description: "Shoes"));

        Assert.Equal(40m, result.Amount);
        Assert.Equal("Clothes", result.CategoryName);
        Assert.Equal(Today.AddDays(-3), result.Date);
    }

    [Fact]
    public async Task UpdateAsync_FutureDate_ThrowsBusinessRule()
    {
        var expense = Db.AddExpense(_maria, _food, 5m, Today);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(_maria, expense.Id, Request(date: Today.AddDays(1))));
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersExpense_ThrowsNotFound()
    {
        var ivans = Db.AddExpense(_ivan, _food, 5m, Today);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(_maria, ivans.Id, Request()));
    }

    [Fact]
    public async Task DeleteAsync_OwnExpense_RemovesIt()
    {
        var expense = Db.AddExpense(_maria, _food, 5m, Today);

        await _sut.DeleteAsync(_maria, expense.Id);

        Assert.Empty(Db.Expenses);
    }

    [Fact]
    public async Task DeleteAsync_AnotherUsersExpense_ThrowsNotFoundAndKeepsIt()
    {
        var ivans = Db.AddExpense(_ivan, _food, 5m, Today);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(_maria, ivans.Id));
        Assert.Single(Db.Expenses);
    }
}
