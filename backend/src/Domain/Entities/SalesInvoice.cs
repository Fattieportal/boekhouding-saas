using Boekhouding.Domain.Common;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Verkoopfactuur
/// </summary>
public class SalesInvoice : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Factuurnummer (uniek per tenant)
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Status van de factuur
    /// </summary>
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    
    /// <summary>
    /// Factuurdatum
    /// </summary>
    public DateTime IssueDate { get; set; }
    
    /// <summary>
    /// Vervaldatum
    /// </summary>
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Contact (klant) voor deze factuur
    /// </summary>
    public Guid ContactId { get; set; }
    
    /// <summary>
    /// Valuta (standaard EUR)
    /// </summary>
    public string Currency { get; set; } = "EUR";
    
    /// <summary>
    /// Subtotaal (excl. BTW)
    /// </summary>
    public decimal Subtotal { get; set; }
    
    /// <summary>
    /// Totaal BTW bedrag
    /// </summary>
    public decimal VatTotal { get; set; }
    
    /// <summary>
    /// Totaalbedrag (incl. BTW)
    /// </summary>
    public decimal Total { get; set; }
    
    /// <summary>
    /// Nog openstaand bedrag (= Total - betaald bedrag)
    /// Wordt automatisch bijgewerkt bij payment matching
    /// </summary>
    public decimal OpenAmount { get; set; }
    
    /// <summary>
    /// Referentie naar het PDF bestand (indien gegenereerd)
    /// </summary>
    public Guid? PdfFileId { get; set; }
    
    /// <summary>
    /// Template gebruikt voor deze factuur (optioneel)
    /// </summary>
    public Guid? TemplateId { get; set; }
    
    /// <summary>
    /// Optionele notities/opmerkingen
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Referentie naar journal entry (als factuur is geboekt)
    /// </summary>
    public Guid? JournalEntryId { get; set; }
    
    // Computed properties (not stored in database)
    
    /// <summary>
    /// Is deze factuur openstaand (unpaid)?
    /// True als Status = Sent/Posted EN OpenAmount > 0
    /// </summary>
    public bool IsUnpaid => 
        (Status == InvoiceStatus.Sent || Status == InvoiceStatus.Posted) && OpenAmount > 0;
    
    /// <summary>
    /// Is deze factuur vervallen (overdue)?
    /// True als Unpaid EN DueDate < vandaag
    /// </summary>
    public bool IsOverdue =>
        IsUnpaid && DueDate.Date < DateTime.UtcNow.Date;
    
    // Navigation
    public Tenant? Tenant { get; set; }
    public Contact? Contact { get; set; }
    public InvoiceTemplate? Template { get; set; }
    public StoredFile? PdfFile { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
}
