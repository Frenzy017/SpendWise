namespace SpendWise.Services.Exceptions;

/// <summary>Thrown when the caller is known but not allowed to do this (HTTP 403).</summary>
public class ForbiddenException(string message) : Exception(message);
