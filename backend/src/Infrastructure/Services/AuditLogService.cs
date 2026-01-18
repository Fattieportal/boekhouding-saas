using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Boekhouding.Infrastructure.Services;

/// <summary>
/// Service implementatie voor audit logging
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        Guid tenantId,
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        object? diff = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow,
            DiffJson = diff != null ? JsonSerializer.Serialize(diff, _jsonOptions) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? entityType = null,
        Guid? entityId = null,
        int skip = 0,
        int take = 100)
    {
        var query = _context.Set<AuditLog>()
            .Include(a => a.ActorUser)
            .Where(a => a.TenantId == tenantId);

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (entityId.HasValue)
        {
            query = query.Where(a => a.EntityId == entityId.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetEntityLogsAsync(
        Guid tenantId,
        string entityType,
        Guid entityId)
    {
        return await _context.Set<AuditLog>()
            .Include(a => a.ActorUser)
            .Where(a => a.TenantId == tenantId
                && a.EntityType == entityType
                && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }
}
