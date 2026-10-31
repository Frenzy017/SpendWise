namespace SpendWise.Web.Models;

/// <summary>Drives the shared previous/next month navigation.</summary>
public record MonthPickerModel(int Year, int Month, string Controller, string Action)
{
    public DateTime Previous => new DateTime(Year, Month, 1).AddMonths(-1);
    public DateTime Next => new DateTime(Year, Month, 1).AddMonths(1);
}
