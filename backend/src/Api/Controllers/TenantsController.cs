using Boekhouding.Application.DTOs.Tenants;
using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Haal alle tenants op waar de huidige gebruiker toegang tot heeft
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<TenantListDto>), 200)]
    public async Task<ActionResult<List<TenantListDto>>> GetMyTenants()
    {
        var userId = GetUserId();
        var tenants = await _tenantService.GetUserTenantsAsync(userId);
        return Ok(tenants);
    }

    /// <summary>
    /// Haal details op van een specifieke tenant
    /// Vereist X-Tenant-Id header
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TenantDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TenantDto>> GetTenant(Guid id)
    {
        var userId = GetUserId();
        
        try
        {
            var tenant = await _tenantService.GetTenantAsync(id, userId);
            return Ok(tenant);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Maak een nieuwe tenant aan en koppel de huidige gebruiker als Admin
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserId();
        var tenant = await _tenantService.CreateTenantAsync(createDto, userId);

        return CreatedAtAction(
            nameof(GetTenant),
            new { id = tenant.Id },
            tenant);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }
}
