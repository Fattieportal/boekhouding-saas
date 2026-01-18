namespace Boekhouding.Application.DTOs.Reports;

/// <summary>
/// DTO voor openstaande debiteuren per klant
/// </summary>
public class AccountsReceivableDto
{
    /// <summary>
    /// Contact ID
    /// </summary>
    public Guid ContactId { get; set; }
    
    /// <summary>
    /// Klantnaam
    /// </summary>
    public string ContactName { get; set; } = string.Empty;
    
    /// <summary>
    /// Totaal openstaand bedrag
    /// </summary>
    public decimal TotalOutstanding { get; set; }
    
    /// <summary>
    /// Aantal openstaande facturen
    /// </summary>
    public int InvoiceCount { get; set; }
    
    /// <summary>
    /// Details van openstaande facturen
    /// </summary>
    public List<OutstandingInvoiceDto> Invoices { get; set; } = new();
}

/// <summary>
/// DTO voor een individuele openstaande factuur
/// </summary>
public class OutstandingInvoiceDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal Outstanding { get; set; }
    public int DaysOverdue { get; set; }
}
