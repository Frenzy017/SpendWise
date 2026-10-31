using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpendWise.Contracts;
using SpendWise.Contracts.Auth;
using SpendWise.Data;
using SpendWise.Data.Entities;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Interfaces;
using SpendWise.Services.Mapping;

namespace SpendWise.Services.Implementations;

public class AuthService(
    SpendWiseDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (!request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
            throw new BusinessRuleException("Password must contain at least one letter and one digit.");

        var normalizedUsername = username.ToUpperInvariant();
        var normalizedEmail = email.ToUpperInvariant();

        var taken = await db.Users.AnyAsync(u =>
            u.NormalizedUsername == normalizedUsername || u.NormalizedEmail == normalizedEmail);
        if (taken)
            throw new ConflictException("Username or email is already in use.");

        var user = new User
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = Roles.User,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);

        // Same message for an unknown user and a wrong password, so usernames can't be probed.
        if (user is null)
            throw new AuthenticationFailedException("Invalid username or password.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new AuthenticationFailedException("Invalid username or password.");

        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync();
        }

        return CreateResponse(user);
    }

    public async Task<UserDto> GetUserAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found.");
        return user.ToDto();
    }

    private AuthResponse CreateResponse(User user)
    {
        var token = tokenService.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAt, user.ToDto());
    }
}
