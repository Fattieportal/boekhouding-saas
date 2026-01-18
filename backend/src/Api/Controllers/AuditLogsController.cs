using Boekhouding.Api.Authorization;
using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Haal audit logs op voor de huidige tenant
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.RequireAccountantOrAdmin)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            {
                return BadRequest(new { error = "Tenant not found in context" });
            }

            var logs = await _auditLogService.GetLogsAsync(
                tenantId.Value,
                startDate,
                endDate,
                entityType,
                entityId,
                skip,
                Math.Min(take, 1000)); // Max 1000 results

            var result = logs.Select(log => new
            {
                log.Id,
                log.Timestamp,
                log.Action,
                log.EntityType,
                log.EntityId,
                Actor = new
                {
                    log.ActorUserId,
                    log.ActorUser.Email
                },
                log.DiffJson,
                log.IpAddress
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for tenant {TenantId}", _tenantContext.TenantId);
            return StatusCode(500, new { error = "Failed to retrieve audit logs" });
        }
    }

    /// <summary>
    /// Haal audit logs op voor een specifieke entiteit
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    [Authorize(Policy = Policies.RequireAccountantOrAdmin)]
    public async Task<IActionResult> GetEntityLogs(string entityType, Guid entityId)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            {
                return BadRequest(new { error = "Tenant not found in context" });
            }

            var logs = await _auditLogService.GetEntityLogsAsync(tenantId.Value, entityType, entityId);

            var result = logs.Select(log => new
            {
                log.Id,
                log.Timestamp,
                log.Action,
                Actor = new
                {
                    log.ActorUserId,
                    log.ActorUser.Email
                },
                log.DiffJson,
                log.IpAddress
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity audit logs for {EntityType} {EntityId}", entityType, entityId);
            return StatusCode(500, new { error = "Failed to retrieve entity audit logs" });
        }
    }
}
