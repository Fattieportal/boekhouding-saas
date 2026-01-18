using Boekhouding.Application.DTOs.Reports;

namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Service voor financiële rapportages
/// </summary>
public interface IFinancialReportService
{
    /// <summary>
    /// Genereer Profit & Loss (Winst & Verlies) rapport
    /// </summary>
    Task<ProfitLossDto> GetProfitLossAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Genereer Balance Sheet (Balans) rapport
    /// </summary>
    Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf, CancellationToken cancellationToken = default);
}
