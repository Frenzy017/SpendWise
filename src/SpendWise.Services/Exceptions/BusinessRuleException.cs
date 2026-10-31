namespace SpendWise.Services.Exceptions;

/// <summary>Thrown when a request breaks a business rule (HTTP 400).</summary>
public class BusinessRuleException(string message) : Exception(message);
