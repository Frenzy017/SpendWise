using Microsoft.AspNetCore.Mvc;
using SpendWise.Api.Infrastructure;
using SpendWise.Contracts.Reports;
using SpendWise.Services.Interfaces;

namespace SpendWise.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsController(IReportService reportService, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>Totals, per-category breakdown and budget status for a month (FR-REP-01, 02).</summary>
    [HttpGet("monthly")]
    [ProducesResponseType<MonthlySummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonthlySummaryDto>> Monthly(int? year, int? month)
    {
        var (y, m) = timeProvider.Resolve(year, month);
        return await reportService.GetMonthlySummaryAsync(User.GetUserId(), y, m);
    }

    /// <summary>Monthly totals for the N months ending with the given month, oldest first (FR-REP-03).</summary>
    [HttpGet("trend")]
    [ProducesResponseType<IReadOnlyList<MonthlyTotalDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MonthlyTotalDto>>> Trend(int? year, int? month, int months = 6)
    {
        var (y, m) = timeProvider.Resolve(year, month);
        return Ok(await reportService.GetTrendAsync(User.GetUserId(), y, m, months));
    }
}
