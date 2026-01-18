using Boekhouding.Domain.Common;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Template voor sales invoices met HTML/CSS en instellingen
/// </summary>
public class InvoiceTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Naam van de template
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Is dit de standaard template voor deze tenant?
    /// </summary>
    public bool IsDefault { get; set; }
    
    /// <summary>
    /// HTML template met placeholders (Handlebars/Liquid syntax)
    /// </summary>
    public string HtmlTemplate { get; set; } = string.Empty;
    
    /// <summary>
    /// CSS styling voor de template
    /// </summary>
    public string CssTemplate { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON met extra instellingen (margins, page size, etc.)
    /// </summary>
    public string? SettingsJson { get; set; }
    
    // Navigation
    public Tenant? Tenant { get; set; }
    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
}
