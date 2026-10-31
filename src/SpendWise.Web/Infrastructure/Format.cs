using System.Globalization;

namespace SpendWise.Web.Infrastructure;

public static class Format
{
    public static string Money(decimal amount) =>
        (amount < 0 ? "-€" : "€") + Math.Abs(amount).ToString("N2", CultureInfo.InvariantCulture);

    public static string MonthName(int year, int month) =>
        new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public static string ShortMonth(int year, int month) =>
        new DateTime(year, month, 1).ToString("MMM yy", CultureInfo.InvariantCulture);
}
