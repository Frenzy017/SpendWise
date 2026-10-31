using SpendWise.Contracts.Auth;

namespace SpendWise.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Creates a User account and returns a token. Throws ConflictException or BusinessRuleException.</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>Checks credentials. Throws AuthenticationFailedException, or ForbiddenException if deactivated.</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task<UserDto> GetUserAsync(int userId);
}
