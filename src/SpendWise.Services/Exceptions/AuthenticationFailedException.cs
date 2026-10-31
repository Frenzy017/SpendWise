namespace SpendWise.Services.Exceptions;

/// <summary>Thrown when login credentials are wrong (HTTP 401).</summary>
public class AuthenticationFailedException(string message) : Exception(message);
