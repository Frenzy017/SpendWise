using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendWise.Web.ApiClient;
using SpendWise.Web.Infrastructure;
using SpendWise.Web.Models;

namespace SpendWise.Web.Controllers;

[Authorize]
public class HomeController(SpendWiseApiClient api) : Controller
{
    /// <summary>Dashboard: monthly summary, category breakdown and 6-month trend (US-10, US-11).</summary>
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var (y, m) = MonthHelper.Resolve(year, month);
        var summary = await api.GetMonthlySummaryAsync(y, m);
        var trend = await api.GetTrendAsync(y, m);
        return View(new DashboardViewModel(summary, trend));
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
