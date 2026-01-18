using Boekhouding.Domain.Common;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Tenant entiteit voor multi-tenant support
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? KvK { get; set; }
    public string? VatNumber { get; set; }

    // Navigation properties
    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
}
