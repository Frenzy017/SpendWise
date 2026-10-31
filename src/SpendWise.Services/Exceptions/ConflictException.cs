namespace SpendWise.Services.Exceptions;

/// <summary>Thrown on duplicates or when a resource is still in use (HTTP 409).</summary>
public class ConflictException(string message) : Exception(message);
