using Boekhouding.Application.DTOs.Dashboard;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Boekhouding.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<DashboardDto> GetDashboardDataAsync(
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId 
            ?? throw new InvalidOperationException("Tenant context is not set");

        var dashboard = new DashboardDto();

        // Get all invoices in one query (more efficient)
        var allInvoices = await _context.Set<SalesInvoice>()
            .Include(i => i.Contact)
            .Where(i => i.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        // Invoice stats
        var unpaidInvoices = allInvoices.Where(i => i.IsUnpaid).ToList();
        var overdueInvoices = unpaidInvoices.Where(i => i.IsOverdue).ToList();
        var paidThisPeriod = allInvoices
            .Where(i => i.Status == InvoiceStatus.Paid 
                     && i.UpdatedAt.HasValue 
                     && i.UpdatedAt.Value >= from 
                     && i.UpdatedAt.Value <= to)
            .ToList();

        dashboard.Invoices = new InvoiceStatsDto
        {
            UnpaidCount = unpaidInvoices.Count,
            OverdueCount = overdueInvoices.Count,
            OpenAmountTotal = unpaidInvoices.Sum(i => i.OpenAmount),
            PaidThisPeriodAmount = paidThisPeriod.Sum(i => i.Total),
            PaidThisPeriodCount = paidThisPeriod.Count
        };

        // Revenue stats (from posted invoices in period)
        var postedInPeriod = allInvoices
            .Where(i => (i.Status == InvoiceStatus.Posted || i.Status == InvoiceStatus.Paid)
                     && i.IssueDate >= from 
                     && i.IssueDate <= to)
            .ToList();

        dashboard.Revenue = new RevenueStatsDto
        {
            RevenueExclThisPeriod = postedInPeriod.Sum(i => i.Subtotal),
            VatThisPeriod = postedInPeriod.Sum(i => i.VatTotal),
            RevenueInclThisPeriod = postedInPeriod.Sum(i => i.Total)
        };

        // Bank stats
        var bankConnections = await _context.Set<BankConnection>()
            .Where(b => b.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var bankTransactions = await _context.Set<BankTransaction>()
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        dashboard.Bank = new BankStatsDto
        {
            LastSyncAt = bankConnections.Any() 
                ? bankConnections.Max(b => b.LastSyncedAt) 
                : null,
            UnmatchedTransactionsCount = bankTransactions.Count(t => t.MatchedStatus == BankTransactionMatchStatus.Unmatched),
            MatchedTransactionsCount = bankTransactions.Count(t => t.MatchedStatus == BankTransactionMatchStatus.MatchedToInvoice)
        };

        // Recent activity (last 10 audit logs)
        var recentLogs = await _context.Set<AuditLog>()
            .Include(a => a.ActorUser)
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToListAsync(cancellationToken);

        dashboard.Activity = recentLogs.Select(log => new RecentActivityDto
        {
            Timestamp = log.Timestamp,
            ActorEmail = log.ActorUser?.Email ?? "System",
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Label = GenerateActivityLabel(log)
        }).ToList();

        // Top customers this period (by revenue)
        var topCustomers = postedInPeriod
            .GroupBy(i => new { i.ContactId, ContactName = i.Contact?.DisplayName ?? "Unknown" })
            .Select(g => new TopCustomerDto
            {
                ContactId = g.Key.ContactId,
                ContactName = g.Key.ContactName,
                TotalRevenue = g.Sum(i => i.Total),
                InvoiceCount = g.Count()
            })
            .OrderByDescending(c => c.TotalRevenue)
            .Take(5)
            .ToList();

        dashboard.TopCustomers = topCustomers;

        return dashboard;
    }

    private string GenerateActivityLabel(AuditLog log)
    {
        return log.Action switch
        {
            "Create" => $"Created {log.EntityType}",
            "Update" => $"Updated {log.EntityType}",
            "Delete" => $"Deleted {log.EntityType}",
            "PostInvoice" => $"Posted invoice",
            "MatchTransaction" => $"Matched bank transaction",
            _ => $"{log.Action} {log.EntityType}"
        };
    }
}
