using Microsoft.IdentityModel.JsonWebTokens;
using SpendWise.Contracts;
using SpendWise.Data.Entities;
using SpendWise.Services.Implementations;
using SpendWise.Services.Options;
using SpendWise.Services.Tests.TestInfrastructure;

namespace SpendWise.Services.Tests;

public class TokenServiceTests : ServiceTestBase
{
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "test-signing-key-that-is-at-least-32-chars!",
            ExpiryMinutes = 120
        });
        _sut = new TokenService(options, Clock);
    }

    [Fact]
    public void CreateToken_ValidUser_ContainsIdNameAndRoleClaims()
    {
        var user = new User { Id = 42, Username = "maria", Role = Roles.Admin };

        var token = new JsonWebTokenHandler().ReadJsonWebToken(_sut.CreateToken(user).Token);

        Assert.Equal("42", token.Subject);
        Assert.Equal("maria", token.GetClaim(JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(Roles.Admin, token.GetClaim("role").Value);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains("test-audience", token.Audiences);
    }

    [Fact]
    public void CreateToken_ValidUser_ExpiresAfterConfiguredMinutes()
    {
        var result = _sut.CreateToken(new User { Id = 1, Username = "u", Role = Roles.User });

        Assert.Equal(Clock.GetUtcNow().UtcDateTime.AddMinutes(120), result.ExpiresAt);
    }
}
