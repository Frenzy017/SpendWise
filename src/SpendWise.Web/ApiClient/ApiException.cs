namespace SpendWise.Web.ApiClient;

/// <summary>A non-success response from the API, built from its ProblemDetails body.</summary>
public class ApiException(int statusCode, string message, IReadOnlyDictionary<string, string[]> errors)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    /// <summary>Field validation errors, keyed by property name (empty for business-rule errors).</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
