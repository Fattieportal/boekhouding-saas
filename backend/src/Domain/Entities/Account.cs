using Boekhouding.Domain.Common;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Grootboekrekening voor dubbel boekhouden
/// </summary>
public class Account : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Unieke code voor de rekening binnen de tenant (bijv. "1000", "8000")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Naam van de rekening (bijv. "Debiteuren", "Omzet")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type rekening: Asset/Liability/Equity/Revenue/Expense
    /// </summary>
    public AccountType Type { get; set; }
    
    /// <summary>
    /// Of de rekening actief is voor gebruik
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
}
