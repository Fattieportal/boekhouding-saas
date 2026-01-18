using Boekhouding.Application.DTOs.TenantBranding;

namespace Boekhouding.Application.Interfaces;

public interface ITenantBrandingService
{
    Task<TenantBrandingDto?> GetBrandingAsync();
    Task<TenantBrandingDto> CreateOrUpdateBrandingAsync(UpdateTenantBrandingDto dto);
}
