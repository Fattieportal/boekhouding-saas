using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Join table tussen User en Tenant met rol per tenant
/// </summary>
public class UserTenant
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public TenantRole Role { get; set; } = TenantRole.Viewer;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
