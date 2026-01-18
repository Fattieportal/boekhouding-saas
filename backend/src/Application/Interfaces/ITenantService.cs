using Boekhouding.Application.DTOs.Tenants;

namespace Boekhouding.Application.Interfaces;

public interface ITenantService
{
    Task<List<TenantListDto>> GetUserTenantsAsync(Guid userId);
    Task<TenantDto> GetTenantAsync(Guid tenantId, Guid userId);
    Task<TenantDto> CreateTenantAsync(CreateTenantDto createDto, Guid userId);
}
