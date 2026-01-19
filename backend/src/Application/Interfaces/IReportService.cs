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
    
    /// <summary>
    /// Export report to PDF
    /// </summary>
    Task<Guid> ExportReportToPdfAsync(string reportType, object reportData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Export report to Excel
    /// </summary>
    Task<Guid> ExportReportToExcelAsync(string reportType, object reportData, CancellationToken cancellationToken = default);
}
