using Boekhouding.Domain.Common;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Factuurlijn in een verkoopfactuur
/// </summary>
public class SalesInvoiceLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Referentie naar de factuur
    /// </summary>
    public Guid InvoiceId { get; set; }
    
    /// <summary>
    /// Regel nummer / volgorde
    /// </summary>
    public int LineNumber { get; set; }
    
    /// <summary>
    /// Omschrijving van het product/dienst
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Aantal
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Prijs per stuk (excl. BTW)
    /// </summary>
    public decimal UnitPrice { get; set; }
    
    /// <summary>
    /// BTW percentage (bijv. 21.00 voor 21%)
    /// </summary>
    public decimal VatRate { get; set; }
    
    /// <summary>
    /// Subtotaal van deze regel (Quantity * UnitPrice)
    /// </summary>
    public decimal LineSubtotal { get; set; }
    
    /// <summary>
    /// BTW bedrag van deze regel
    /// </summary>
    public decimal LineVatAmount { get; set; }
    
    /// <summary>
    /// Totaal van deze regel (incl. BTW)
    /// </summary>
    public decimal LineTotal { get; set; }
    
    /// <summary>
    /// Optionele referentie naar account voor boeking
    /// </summary>
    public Guid? AccountId { get; set; }
    
    // Navigation
    public SalesInvoice? Invoice { get; set; }
    public Account? Account { get; set; }
}
