using Boekhouding.Api.Authorization;
using Boekhouding.Application.DTOs.TenantBranding;
using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantBrandingController : ControllerBase
{
    private readonly ITenantBrandingService _service;

    public TenantBrandingController(ITenantBrandingService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<TenantBrandingDto>> Get()
    {
        var branding = await _service.GetBrandingAsync();
        if (branding == null)
            return NotFound(new { message = "No branding configured for this tenant" });

        return Ok(branding);
    }

    [HttpPut]
    [Authorize(Policy = Policies.RequireAdminOrOwner)]
    public async Task<ActionResult<TenantBrandingDto>> CreateOrUpdate([FromBody] UpdateTenantBrandingDto dto)
    {
        var branding = await _service.CreateOrUpdateBrandingAsync(dto);
        return Ok(branding);
    }
}
