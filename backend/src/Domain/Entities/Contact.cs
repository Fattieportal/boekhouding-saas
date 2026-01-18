using Boekhouding.Domain.Common;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Contact entiteit voor klanten en leveranciers
/// </summary>
public class Contact : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Type contact: Customer, Supplier of Both
    /// </summary>
    public ContactType Type { get; set; }
    
    /// <summary>
    /// Weergavenaam van het contact
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Email adres
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Telefoonnummer
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// Adresregel 1
    /// </summary>
    public string? AddressLine1 { get; set; }
    
    /// <summary>
    /// Adresregel 2
    /// </summary>
    public string? AddressLine2 { get; set; }
    
    /// <summary>
    /// Postcode
    /// </summary>
    public string? PostalCode { get; set; }
    
    /// <summary>
    /// Plaats
    /// </summary>
    public string? City { get; set; }
    
    /// <summary>
    /// Land (default NL)
    /// </summary>
    public string Country { get; set; } = "NL";
    
    /// <summary>
    /// BTW nummer (optioneel)
    /// </summary>
    public string? VatNumber { get; set; }
    
    /// <summary>
    /// KvK nummer (optioneel)
    /// </summary>
    public string? KvK { get; set; }
    
    /// <summary>
    /// Of het contact actief is
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
}
