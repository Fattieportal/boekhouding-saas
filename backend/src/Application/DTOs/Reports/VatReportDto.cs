namespace Boekhouding.Application.DTOs.Reports;

/// <summary>
/// BTW rapportage per aangifteperiode
/// </summary>
public class VatReportDto
{
    /// <summary>
    /// Start datum van de periode
    /// </summary>
    public DateTime FromDate { get; set; }
    
    /// <summary>
    /// Eind datum van de periode
    /// </summary>
    public DateTime ToDate { get; set; }
    
    /// <summary>
    /// BTW per tarief
    /// </summary>
    public List<VatRateBreakdownDto> VatRates { get; set; } = new();
    
    /// <summary>
    /// Totaal omzet (excl. BTW)
    /// </summary>
    public decimal TotalRevenue { get; set; }
    
    /// <summary>
    /// Totaal BTW bedrag
    /// </summary>
    public decimal TotalVat { get; set; }
    
    /// <summary>
    /// Totaal inclusief BTW
    /// </summary>
    public decimal TotalIncludingVat { get; set; }
    
    /// <summary>
    /// Aantal gefactureerde verkoopfacturen in de periode
    /// </summary>
    public int InvoiceCount { get; set; }
}

/// <summary>
/// BTW uitsplitsing per tarief
/// </summary>
public class VatRateBreakdownDto
{
    /// <summary>
    /// BTW tarief (bijv. 0, 9, 21)
    /// </summary>
    public decimal VatRate { get; set; }
    
    /// <summary>
    /// Omzet exclusief BTW voor dit tarief
    /// </summary>
    public decimal Revenue { get; set; }
    
    /// <summary>
    /// BTW bedrag voor dit tarief
    /// </summary>
    public decimal VatAmount { get; set; }
    
    /// <summary>
    /// Aantal factuurregels met dit tarief
    /// </summary>
    public int LineCount { get; set; }
}
