using Boekhouding.Application.DTOs.Reports;

namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Service voor rapportages
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Haal openstaande debiteuren op per klant
    /// </summary>
    Task<List<AccountsReceivableDto>> GetAccountsReceivableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Haal BTW rapportage op voor een periode (gebaseerd op geboekte facturen)
    /// </summary>
    Task<VatReportDto> GetVatReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
