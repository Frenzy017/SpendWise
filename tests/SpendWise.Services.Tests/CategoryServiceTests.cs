using SpendWise.Contracts;
using SpendWise.Contracts.Categories;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Implementations;
using SpendWise.Services.Tests.TestInfrastructure;

namespace SpendWise.Services.Tests;

public class CategoryServiceTests : ServiceTestBase
{
    private readonly CategoryService _sut;
    private readonly int _maria;
    private readonly int _ivan;
    private readonly int _admin;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(Db, Clock);
        _maria = Db.AddUser("maria").Id;
        _ivan = Db.AddUser("ivan").Id;
        _admin = Db.AddUser("admin", Roles.Admin).Id;
    }

    private static CategoryRequest Request(string name, string color = "#aabbcc") => new(name, color);

    [Fact]
    public async Task GetVisibleAsync_ReturnsGlobalAndOwnButNotOthers_SortedByName()
    {
        Db.AddCategory("Food");
        Db.AddCategory("gym", _maria);
        Db.AddCategory("Clothes");
        Db.AddCategory("Fishing", _ivan);

        var result = await _sut.GetVisibleAsync(_maria);

        Assert.Equal(["Clothes", "Food", "gym"], result.Select(c => c.Name));
        Assert.False(result.Single(c => c.Name == "gym").IsGlobal);
        Assert.True(result.Single(c => c.Name == "Food").IsGlobal);
    }

    [Fact]
    public async Task CreateAsync_AsUser_CreatesPersonalCategory()
    {
        var result = await _sut.CreateAsync(_maria, isAdmin: false, Request(" Gym "));

        Assert.Equal("Gym", result.Name);
        Assert.Equal("#AABBCC", result.Color);
        Assert.False(result.IsGlobal);
        Assert.Equal(_maria, Db.Categories.Single().UserId);
    }

    [Fact]
    public async Task CreateAsync_AsAdmin_CreatesGlobalCategory()
    {
        var result = await _sut.CreateAsync(_admin, isAdmin: true, Request("Travel"));

        Assert.True(result.IsGlobal);
        Assert.Null(Db.Categories.Single().UserId);
    }

    [Fact]
    public async Task CreateAsync_NameMatchesGlobalIgnoringCase_ThrowsConflict()
    {
        Db.AddCategory("Food");

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(_maria, false, Request("FOOD")));
    }

    [Fact]
    public async Task CreateAsync_NameMatchesOwnCategory_ThrowsConflict()
    {
        Db.AddCategory("Gym", _maria);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(_maria, false, Request("gym")));
    }

    [Fact]
    public async Task CreateAsync_NameMatchesAnotherUsersCategory_Succeeds()
    {
        Db.AddCategory("Gym", _ivan);

        var result = await _sut.CreateAsync(_maria, false, Request("Gym"));

        Assert.Equal("Gym", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_OwnCategory_UpdatesNameAndColor()
    {
        var category = Db.AddCategory("Gym", _maria);

        var result = await _sut.UpdateAsync(_maria, false, category.Id, Request("Fitness", "#000000"));

        Assert.Equal("Fitness", result.Name);
        Assert.Equal("#000000", result.Color);
    }

    [Fact]
    public async Task UpdateAsync_SameNameDifferentCase_Succeeds()
    {
        var category = Db.AddCategory("gym", _maria);

        var result = await _sut.UpdateAsync(_maria, false, category.Id, Request("Gym"));

        Assert.Equal("Gym", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_RenameToExistingName_ThrowsConflict()
    {
        Db.AddCategory("Food");
        var category = Db.AddCategory("Gym", _maria);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpdateAsync(_maria, false, category.Id, Request("food")));
    }

    [Fact]
    public async Task UpdateAsync_GlobalCategoryAsUser_ThrowsForbidden()
    {
        var food = Db.AddCategory("Food");

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.UpdateAsync(_maria, false, food.Id, Request("Groceries")));
    }

    [Fact]
    public async Task UpdateAsync_GlobalCategoryAsAdmin_Updates()
    {
        var food = Db.AddCategory("Food");

        var result = await _sut.UpdateAsync(_admin, true, food.Id, Request("Groceries"));

        Assert.Equal("Groceries", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersCategory_ThrowsNotFound()
    {
        var ivans = Db.AddCategory("Fishing", _ivan);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(_maria, false, ivans.Id, Request("Mine")));
    }

    [Fact]
    public async Task DeleteAsync_UnusedOwnCategory_RemovesIt()
    {
        var category = Db.AddCategory("Gym", _maria);

        await _sut.DeleteAsync(_maria, false, category.Id);

        Assert.Empty(Db.Categories);
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithExpenses_ThrowsConflict()
    {
        var category = Db.AddCategory("Gym", _maria);
        Db.AddExpense(_maria, category.Id, 10m, Today);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(_maria, false, category.Id));
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithBudget_ThrowsConflict()
    {
        var category = Db.AddCategory("Gym", _maria);
        Db.AddBudget(_maria, category.Id, 2026, 10, 50m);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(_maria, false, category.Id));
    }

    [Fact]
    public async Task DeleteAsync_GlobalCategoryUsedByAnyUser_AsAdmin_ThrowsConflict()
    {
        var food = Db.AddCategory("Food");
        Db.AddExpense(_ivan, food.Id, 10m, Today);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(_admin, true, food.Id));
    }

    [Fact]
    public async Task DeleteAsync_GlobalCategoryAsUser_ThrowsForbidden()
    {
        var food = Db.AddCategory("Food");

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeleteAsync(_maria, false, food.Id));
    }
}
