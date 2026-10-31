namespace SpendWise.Contracts;

/// <summary>Limits from spec.md, shared by the API (validation) and the client (form hints).</summary>
public static class ValidationLimits
{
    public const double MinAmount = 0.01;
    public const double MaxAmount = 1_000_000;

    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 32;
    public const string UsernamePattern = @"^[A-Za-z0-9_.]+$";
    public const int EmailMaxLength = 256;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 100;

    public const int CategoryNameMaxLength = 40;
    public const string ColorPattern = "^#[0-9A-Fa-f]{6}$";
    public const int DescriptionMaxLength = 200;

    public const int MinYear = 2000;
    public const int MaxYear = 2100;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxTrendMonths = 12;
}
