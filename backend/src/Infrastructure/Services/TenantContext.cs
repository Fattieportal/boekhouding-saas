using Boekhouding.Application.Interfaces;

namespace Boekhouding.Infrastructure.Services;

/// <summary>
/// Scoped service die de huidige tenant context bijhoudt per request
/// </summary>
public class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    public Guid? TenantId => _tenantId;

    public void SetTenantId(Guid tenantId)
    {
        if (_tenantId.HasValue && _tenantId.Value != tenantId)
        {
            throw new InvalidOperationException("TenantId cannot be changed once set for the current request.");
        }

        _tenantId = tenantId;
    }
}
