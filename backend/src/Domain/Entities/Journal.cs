using Boekhouding.Domain.Common;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Dagboek voor het groeperen van boekingen
/// </summary>
public class Journal : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Unieke code voor het dagboek binnen de tenant (bijv. "VRK", "INK", "BANK")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Naam van het dagboek (bijv. "Verkopen", "Inkopen", "Bank")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type dagboek: Sales/Purchase/Bank/General
    /// </summary>
    public JournalType Type { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
}
