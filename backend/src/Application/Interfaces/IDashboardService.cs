using Boekhouding.Application.DTOs.Dashboard;

namespace Boekhouding.Application.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Get dashboard statistics for the current tenant
    /// </summary>
    Task<DashboardDto> GetDashboardDataAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
