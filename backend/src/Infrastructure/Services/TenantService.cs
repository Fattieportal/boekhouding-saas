using Boekhouding.Application.DTOs.Tenants;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _context;

    public TenantService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantListDto>> GetUserTenantsAsync(Guid userId)
    {
        var tenants = await _context.UserTenants
            .Where(ut => ut.UserId == userId)
            .Include(ut => ut.Tenant)
            .Select(ut => new TenantListDto
            {
                Id = ut.TenantId,
                Name = ut.Tenant.Name,
                Role = ut.Role.ToString()
            })
            .ToListAsync();

        return tenants;
    }

    public async Task<TenantDto> GetTenantAsync(Guid tenantId, Guid userId)
    {
        var userTenant = await _context.UserTenants
            .Include(ut => ut.Tenant)
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TenantId == tenantId);

        if (userTenant == null)
        {
            throw new UnauthorizedAccessException("User does not have access to this tenant");
        }

        return new TenantDto
        {
            Id = userTenant.Tenant.Id,
            Name = userTenant.Tenant.Name,
            KvK = userTenant.Tenant.KvK,
            VatNumber = userTenant.Tenant.VatNumber,
            CreatedAt = userTenant.Tenant.CreatedAt,
            Role = userTenant.Role.ToString()
        };
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantDto createDto, Guid userId)
    {
        // Valideer of gebruiker bestaat
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Maak nieuwe tenant aan
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            KvK = createDto.KvK,
            VatNumber = createDto.VatNumber,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);

        // Koppel gebruiker als Admin aan tenant
        var userTenant = new UserTenant
        {
            UserId = userId,
            TenantId = tenant.Id,
            Role = TenantRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserTenants.Add(userTenant);

        await _context.SaveChangesAsync();

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            KvK = tenant.KvK,
            VatNumber = tenant.VatNumber,
            CreatedAt = tenant.CreatedAt,
            Role = TenantRole.Admin.ToString()
        };
    }
}
