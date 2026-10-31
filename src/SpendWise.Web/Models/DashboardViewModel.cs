using SpendWise.Contracts.Reports;

namespace SpendWise.Web.Models;

public record DashboardViewModel(MonthlySummaryDto Summary, IReadOnlyList<MonthlyTotalDto> Trend);
