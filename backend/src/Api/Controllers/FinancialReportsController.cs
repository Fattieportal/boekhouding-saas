using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

/// <summary>
/// Financial Reports controller
/// </summary>
[Authorize]
[ApiController]
[Route("api/reports")]
public class FinancialReportsController : ControllerBase
{
    private readonly IFinancialReportService _reportService;

    public FinancialReportsController(IFinancialReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Get Profit & Loss (Winst & Verlies) report
    /// </summary>
    /// <param name="from">Start date (defaults to start of current year)</param>
    /// <param name="to">End date (defaults to today)</param>
    [HttpGet("profit-loss")]
    public async Task<IActionResult> GetProfitLoss(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        // Ensure DateTime params are UTC for PostgreSQL compatibility
        var fromDate = from.HasValue 
            ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) 
            : new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = to.HasValue 
            ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) 
            : DateTime.UtcNow;

        var report = await _reportService.GetProfitLossAsync(fromDate, toDate, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Get Balance Sheet (Balans) report
    /// </summary>
    /// <param name="asOf">Date as of which to calculate balances (defaults to today)</param>
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet(
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
    {
        // Ensure DateTime param is UTC for PostgreSQL compatibility
        var asOfDate = asOf.HasValue 
            ? DateTime.SpecifyKind(asOf.Value, DateTimeKind.Utc) 
            : DateTime.UtcNow;

        var report = await _reportService.GetBalanceSheetAsync(asOfDate, cancellationToken);
        return Ok(report);
    }
}
