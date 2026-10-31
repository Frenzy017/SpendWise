using SpendWise.Contracts.Reports;

namespace SpendWise.Services.Interfaces;

public interface IReportService
{
    Task<MonthlySummaryDto> GetMonthlySummaryAsync(int userId, int year, int month);

    /// <summary>Monthly totals for a number of months ending with year/month, oldest first.</summary>
    Task<IReadOnlyList<MonthlyTotalDto>> GetTrendAsync(int userId, int year, int month, int months);
}
