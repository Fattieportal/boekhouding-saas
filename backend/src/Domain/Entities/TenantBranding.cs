using Boekhouding.Domain.Common;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Branding instellingen per tenant voor facturen en andere documenten
/// </summary>
public class TenantBranding : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// URL naar logo bestand
    /// </summary>
    public string? LogoUrl { get; set; }
    
    /// <summary>
    /// Primaire kleur (hex formaat, bijv. #0066CC)
    /// </summary>
    public string? PrimaryColor { get; set; }
    
    /// <summary>
    /// Secundaire kleur (hex formaat)
    /// </summary>
    public string? SecondaryColor { get; set; }
    
    /// <summary>
    /// Font familie voor documenten
    /// </summary>
    public string? FontFamily { get; set; }
    
    /// <summary>
    /// Footer tekst voor facturen (bijv. BTW nummer, KvK nummer, bankgegevens)
    /// </summary>
    public string? FooterText { get; set; }
    
    // Navigation
    public Tenant? Tenant { get; set; }
}
