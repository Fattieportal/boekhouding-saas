namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Interface voor het bepalen van de huidige tenant context
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    void SetTenantId(Guid tenantId);
}
