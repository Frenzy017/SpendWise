using SpendWise.Contracts;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Implementations;
using SpendWise.Services.Tests.TestInfrastructure;

namespace SpendWise.Services.Tests;

public class UserAdminServiceTests : ServiceTestBase
{
    private readonly UserAdminService _sut;
    private readonly int _admin;
    private readonly int _maria;

    public UserAdminServiceTests()
    {
        _sut = new UserAdminService(Db);
        _admin = Db.AddUser("admin", Roles.Admin).Id;
        _maria = Db.AddUser("maria").Id;
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsAllUsersWithExpenseCountsSortedByUsername()
    {
        var food = Db.AddCategory("Food");
        Db.AddExpense(_maria, food.Id, 10m, Today);
        Db.AddExpense(_maria, food.Id, 20m, Today);

        var result = await _sut.GetUsersAsync();

        Assert.Equal(["admin", "maria"], result.Select(u => u.Username));
        Assert.Equal(2, result.Single(u => u.Id == _maria).ExpenseCount);
        Assert.Equal(0, result.Single(u => u.Id == _admin).ExpenseCount);
    }

    [Fact]
    public async Task SetStatusAsync_Deactivate_MarksUserInactive()
    {
        await _sut.SetStatusAsync(_admin, _maria, isActive: false);

        Assert.False((await Db.Users.FindAsync(_maria))!.IsActive);
    }

    [Fact]
    public async Task SetStatusAsync_Reactivate_MarksUserActive()
    {
        var blocked = Db.AddUser("blocked", isActive: false);

        await _sut.SetStatusAsync(_admin, blocked.Id, isActive: true);

        Assert.True((await Db.Users.FindAsync(blocked.Id))!.IsActive);
    }

    [Fact]
    public async Task SetStatusAsync_OwnAccount_ThrowsBusinessRule()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetStatusAsync(_admin, _admin, isActive: false));
    }

    [Fact]
    public async Task SetStatusAsync_UnknownUser_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.SetStatusAsync(_admin, 999, isActive: false));
    }
}
