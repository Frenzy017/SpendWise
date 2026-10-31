namespace SpendWise.Api.Infrastructure;

public static class MonthDefaults
{
    /// <summary>Fills in a missing year or month with the current one.</summary>
    public static (int Year, int Month) Resolve(this TimeProvider timeProvider, int? year, int? month)
    {
        var today = timeProvider.GetLocalNow();
        return (year ?? today.Year, month ?? today.Month);
    }
}
