using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Boekhouding.Api.Extensions;

/// <summary>
/// Extension methods voor audit logging in controllers
/// </summary>
public static class AuditLogExtensions
{
    /// <summary>
    /// Log een audit event met automatische IP en User Agent capture
    /// </summary>
    public static async Task LogAuditAsync(
        this IAuditLogService auditLogService,
        HttpContext httpContext,
        Guid tenantId,
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        object? diff = null)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
            ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? "unknown";
            
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        
        await auditLogService.LogAsync(
            tenantId,
            actorUserId,
            action,
            entityType,
            entityId,
            diff,
            ipAddress,
            userAgent);
    }
}
