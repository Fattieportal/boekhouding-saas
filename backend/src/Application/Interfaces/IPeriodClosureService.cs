using Boekhouding.Application.DTOs.PeriodClosure;

namespace Boekhouding.Application.Interfaces;

public interface IPeriodClosureService
{
    /// <summary>
    /// Close an accounting period
    /// </summary>
    Task<PeriodClosureResponse> ClosePeriodAsync(int year, int month, string closedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reopen a closed accounting period
    /// </summary>
    Task<PeriodReopenResponse> ReopenPeriodAsync(int year, int month, string reason, string reopenedBy, CancellationToken cancellationToken = default);
}
