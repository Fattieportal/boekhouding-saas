using Boekhouding.Domain.Common;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Bank connectie voor PSD2 integratie
/// </summary>
public class BankConnection : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Provider naam (bijv. "Mock", "Plaid", "Nordigen")
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Status van de connectie
    /// </summary>
    public BankConnectionStatus Status { get; set; } = BankConnectionStatus.Pending;
    
    /// <summary>
    /// Versleutelde access token
    /// </summary>
    public string? AccessTokenEncrypted { get; set; }
    
    /// <summary>
    /// Versleutelde refresh token
    /// </summary>
    public string? RefreshTokenEncrypted { get; set; }
    
    /// <summary>
    /// Vervaldatum van de tokens
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Externe ID bij de provider
    /// </summary>
    public string? ExternalConnectionId { get; set; }
    
    /// <summary>
    /// Banknaam (voor display)
    /// </summary>
    public string? BankName { get; set; }
    
    /// <summary>
    /// IBAN nummer (gemaskeerd voor display)
    /// </summary>
    public string? IbanMasked { get; set; }
    
    /// <summary>
    /// Laatste sync timestamp
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
}
