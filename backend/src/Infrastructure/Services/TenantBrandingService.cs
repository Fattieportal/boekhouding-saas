using Boekhouding.Application.DTOs.TenantBranding;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class TenantBrandingService : ITenantBrandingService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public TenantBrandingService(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<TenantBrandingDto?> GetBrandingAsync()
    {
        var branding = await _context.Set<TenantBranding>()
            .FirstOrDefaultAsync();

        return branding != null ? MapToDto(branding) : null;
    }

    public async Task<TenantBrandingDto> CreateOrUpdateBrandingAsync(UpdateTenantBrandingDto dto)
    {
        var tenantId = _tenantContext.TenantId 
            ?? throw new InvalidOperationException("TenantId is required");

        var branding = await _context.Set<TenantBranding>()
            .FirstOrDefaultAsync();

        if (branding == null)
        {
            // Create new
            branding = new TenantBranding
            {
                TenantId = tenantId,
                LogoUrl = dto.LogoUrl,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                FontFamily = dto.FontFamily,
                FooterText = dto.FooterText
            };

            _context.Set<TenantBranding>().Add(branding);
        }
        else
        {
            // Update existing - ensure TenantId is set (fix for old records)
            branding.TenantId = tenantId;
            if (dto.LogoUrl != null) branding.LogoUrl = dto.LogoUrl;
            if (dto.PrimaryColor != null) branding.PrimaryColor = dto.PrimaryColor;
            if (dto.SecondaryColor != null) branding.SecondaryColor = dto.SecondaryColor;
            if (dto.FontFamily != null) branding.FontFamily = dto.FontFamily;
            if (dto.FooterText != null) branding.FooterText = dto.FooterText;
        }

        await _context.SaveChangesAsync();

        return MapToDto(branding);
    }

    private static TenantBrandingDto MapToDto(TenantBranding branding)
    {
        return new TenantBrandingDto
        {
            Id = branding.Id,
            TenantId = branding.TenantId,
            LogoUrl = branding.LogoUrl,
            PrimaryColor = branding.PrimaryColor,
            SecondaryColor = branding.SecondaryColor,
            FontFamily = branding.FontFamily,
            FooterText = branding.FooterText,
            CreatedAt = branding.CreatedAt,
            UpdatedAt = branding.UpdatedAt
        };
    }
}
