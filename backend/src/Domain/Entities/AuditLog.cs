using Boekhouding.Domain.Common;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Audit log entiteit voor tracking van belangrijke acties in het systeem
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Tenant waarvoor de actie werd uitgevoerd
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Gebruiker die de actie uitvoerde
    /// </summary>
    public Guid ActorUserId { get; set; }
    
    /// <summary>
    /// Actie die werd uitgevoerd (bijv. "Create", "Update", "Delete", "Post", "Reverse")
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Type entiteit waarop de actie werd uitgevoerd
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID van de entiteit waarop de actie werd uitgevoerd
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// Timestamp van de actie (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// JSON met veranderingen (before/after voor updates, of volledige state voor creates)
    /// </summary>
    public string? DiffJson { get; set; }
    
    /// <summary>
    /// IP adres van de gebruiker
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent van de request
    /// </summary>
    public string? UserAgent { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public User ActorUser { get; set; } = null!;
}
