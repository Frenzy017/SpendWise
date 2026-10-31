using Microsoft.AspNetCore.Identity;
using SpendWise.Contracts;
using SpendWise.Contracts.Auth;
using SpendWise.Data.Entities;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Implementations;
using SpendWise.Services.Options;
using SpendWise.Services.Tests.TestInfrastructure;

namespace SpendWise.Services.Tests;

public class AuthServiceTests : ServiceTestBase
{
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var jwt = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "i", Audience = "a", Key = "test-signing-key-that-is-at-least-32-chars!", ExpiryMinutes = 120
        });
        _sut = new AuthService(Db, new PasswordHasher<User>(), new TokenService(jwt, Clock), Clock);
    }

    private Task<AuthResponse> RegisterAsync(string username = "maria", string email = "maria@test.com", string password = "Secret123") =>
        _sut.RegisterAsync(new RegisterRequest(username, email, password));

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserRoleWithHashedPassword()
    {
        var response = await RegisterAsync();

        var user = Db.Users.Single();
        Assert.Equal("maria", user.Username);
        Assert.Equal(Roles.User, user.Role);
        Assert.True(user.IsActive);
        Assert.NotEqual("Secret123", user.PasswordHash);
        Assert.Equal(Roles.User, response.User.Role);
        Assert.False(string.IsNullOrEmpty(response.Token));
    }

    [Fact]
    public async Task RegisterAsync_UsernameTakenWithDifferentCase_ThrowsConflict()
    {
        await RegisterAsync();

        await Assert.ThrowsAsync<ConflictException>(() => RegisterAsync(username: "MARIA", email: "other@test.com"));
    }

    [Fact]
    public async Task RegisterAsync_EmailTakenWithDifferentCase_ThrowsConflict()
    {
        await RegisterAsync();

        await Assert.ThrowsAsync<ConflictException>(() => RegisterAsync(username: "other", email: "Maria@Test.com"));
    }

    [Theory]
    [InlineData("onlyletters")]
    [InlineData("12345678")]
    public async Task RegisterAsync_PasswordWithoutLetterOrDigit_ThrowsBusinessRule(string password)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => RegisterAsync(password: password));
        Assert.Empty(Db.Users);
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentialsAnyUsernameCase_ReturnsTokenAndUser()
    {
        await RegisterAsync();

        var response = await _sut.LoginAsync(new LoginRequest("Maria", "Secret123"));

        Assert.Equal("maria", response.User.Username);
        Assert.False(string.IsNullOrEmpty(response.Token));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsAuthenticationFailed()
    {
        await RegisterAsync();

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _sut.LoginAsync(new LoginRequest("maria", "Wrong1234")));
        Assert.Equal("Invalid username or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_ThrowsSameGenericError()
    {
        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _sut.LoginAsync(new LoginRequest("nobody", "Secret123")));
        Assert.Equal("Invalid username or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedUser_ThrowsForbidden()
    {
        await RegisterAsync();
        Db.Users.Single().IsActive = false;
        await Db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.LoginAsync(new LoginRequest("maria", "Secret123")));
    }

    [Fact]
    public async Task GetUserAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetUserAsync(999));
    }
}
