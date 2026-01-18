namespace Boekhouding.Domain.Common;

/// <summary>
/// Interface voor entiteiten die tenant-owned zijn
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
