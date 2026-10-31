namespace SpendWise.Contracts.Auth;

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
