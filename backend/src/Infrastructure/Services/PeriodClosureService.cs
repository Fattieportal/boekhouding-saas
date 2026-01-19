using Boekhouding.Application.DTOs.PeriodClosure;
using Boekhouding.Application.Interfaces;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class PeriodClosureService : IPeriodClosureService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLog;
    private readonly IUserContext _userContext;

    public PeriodClosureService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        IAuditLogService auditLog,
        IUserContext userContext)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditLog = auditLog;
        _userContext = userContext;
    }

    public async Task<PeriodClosureResponse> ClosePeriodAsync(int year, int month, string closedBy, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        var closureId = Guid.NewGuid();
        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // Count entries in period
        var entriesInPeriod = await _context.JournalEntries
            .Where(je => je.TenantId == tenantId &&
                        je.EntryDate >= periodStart &&
                        je.EntryDate <= periodEnd)
            .CountAsync(cancellationToken);

        // Audit log for period closure
        await _auditLog.LogAsync(tenantId, userId, "CLOSE_PERIOD", "Period", closureId,
            new 
            { 
                Year = year,
                Month = month,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ClosedBy = closedBy,
                ClosedAt = DateTime.UtcNow,
                EntriesInPeriod = entriesInPeriod,
                Message = $"Accounting period {year:0000}-{month:00} closed by {closedBy} ({entriesInPeriod} entries)"
            });

        return new PeriodClosureResponse
        {
            ClosureId = closureId,
            Year = year,
            Month = month,
            ClosedAt = DateTime.UtcNow,
            ClosedBy = closedBy,
            EntriesInPeriod = entriesInPeriod,
            Status = "Closed"
        };
    }

    public async Task<PeriodReopenResponse> ReopenPeriodAsync(int year, int month, string reason, string reopenedBy, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        var reopenId = Guid.NewGuid();
        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // Audit log for period reopening
        await _auditLog.LogAsync(tenantId, userId, "REOPEN_PERIOD", "Period", reopenId,
            new 
            { 
                Year = year,
                Month = month,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ReopenedBy = reopenedBy,
                ReopenedAt = DateTime.UtcNow,
                Reason = reason,
                Message = $"⚠️ Accounting period {year:0000}-{month:00} reopened by {reopenedBy}: {reason}"
            });

        return new PeriodReopenResponse
        {
            ReopenId = reopenId,
            Year = year,
            Month = month,
            ReopenedAt = DateTime.UtcNow,
            ReopenedBy = reopenedBy,
            Reason = reason,
            Status = "Reopened"
        };
    }
}
