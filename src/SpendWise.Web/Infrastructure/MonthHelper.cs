namespace SpendWise.Web.Infrastructure;

public static class MonthHelper
{
    /// <summary>Uses the given month if valid, otherwise the current one.</summary>
    public static (int Year, int Month) Resolve(int? year, int? month)
    {
        var today = DateTime.Today;
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        return y is >= 2000 and <= 2100 && m is >= 1 and <= 12 ? (y, m) : (today.Year, today.Month);
    }
}
