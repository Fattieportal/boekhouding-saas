using Boekhouding.Domain.Entities;

namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Service voor het loggen van audit trails
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Log een actie voor audit purposes
    /// </summary>
    Task LogAsync(
        Guid tenantId,
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        object? diff = null,
        string? ipAddress = null,
        string? userAgent = null);
    
    /// <summary>
    /// Haal audit logs op voor een specifieke tenant
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? entityType = null,
        Guid? entityId = null,
        int skip = 0,
        int take = 100);
    
    /// <summary>
    /// Haal audit logs op voor een specifieke entiteit
    /// </summary>
    Task<IEnumerable<AuditLog>> GetEntityLogsAsync(
        Guid tenantId,
        string entityType,
        Guid entityId);
}
