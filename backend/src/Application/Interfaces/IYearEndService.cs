using Boekhouding.Application.DTOs.YearEnd;

namespace Boekhouding.Application.Interfaces;

public interface IYearEndService
{
    /// <summary>
    /// Close the fiscal year and transfer result to equity
    /// </summary>
    Task<YearEndCloseResponse> CloseYearAsync(int year, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create opening balances for new fiscal year
    /// </summary>
    Task<OpeningBalancesResponse> CreateOpeningBalancesAsync(int year, CancellationToken cancellationToken = default);
}
