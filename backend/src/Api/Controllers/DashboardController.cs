using Boekhouding.Application.DTOs.Dashboard;
using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get dashboard statistics for the current tenant
    /// </summary>
    /// <param name="from">Start date for period calculations (defaults to start of current month)</param>
    /// <param name="to">End date for period calculations (defaults to today)</param>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> GetDashboard(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        // Default to current month if not specified
        var fromDate = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = to ?? DateTime.UtcNow;

        var dashboard = await _dashboardService.GetDashboardDataAsync(fromDate, toDate, cancellationToken);
        return Ok(dashboard);
    }
}
