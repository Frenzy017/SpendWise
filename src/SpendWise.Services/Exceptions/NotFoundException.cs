namespace SpendWise.Services.Exceptions;

/// <summary>Thrown when a resource does not exist or belongs to another user (HTTP 404).</summary>
public class NotFoundException(string message) : Exception(message);
