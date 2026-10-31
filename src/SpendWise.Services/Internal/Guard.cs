using SpendWise.Contracts;
using SpendWise.Services.Exceptions;

namespace SpendWise.Services.Internal;

internal static class Guard
{
    public static void ValidMonth(int year, int month)
    {
        if (year < ValidationLimits.MinYear || year > ValidationLimits.MaxYear)
            throw new BusinessRuleException($"Year must be between {ValidationLimits.MinYear} and {ValidationLimits.MaxYear}.");
        if (month is < 1 or > 12)
            throw new BusinessRuleException("Month must be between 1 and 12.");
    }

    public static void ValidAmount(decimal amount, string fieldName)
    {
        if (amount < (decimal)ValidationLimits.MinAmount || amount > (decimal)ValidationLimits.MaxAmount)
            throw new BusinessRuleException($"{fieldName} must be between 0.01 and 1,000,000.");
    }

    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}
