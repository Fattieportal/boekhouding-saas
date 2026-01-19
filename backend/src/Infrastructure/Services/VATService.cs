using Boekhouding.Application.DTOs.VAT;
using Boekhouding.Application.Interfaces;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class VATService : IVATService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLog;
    private readonly IUserContext _userContext;

    public VATService(
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

    public async Task<VATCalculationResponse> CalculateVATAsync(int year, int quarter, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        // Calculate period dates
        var periodStart = new DateTime(year, (quarter - 1) * 3 + 1, 1);
        var periodEnd = periodStart.AddMonths(3).AddDays(-1);

        // Calculate VAT from journal entries (simplified - real implementation would be more complex)
        var journalEntries = await _context.JournalEntries
            .Where(je => je.TenantId == tenantId && 
                        je.EntryDate >= periodStart && 
                        je.EntryDate <= periodEnd &&
                        je.Status == Domain.Enums.JournalEntryStatus.Posted)
            .Include(je => je.Lines)
                .ThenInclude(line => line.Account)
            .ToListAsync(cancellationToken);

        decimal salesVAT = 0;
        decimal purchaseVAT = 0;

        // Simplified calculation - real implementation would use proper VAT accounts
        foreach (var entry in journalEntries)
        {
            foreach (var line in entry.Lines)
            {
                // Simplified: assume accounts with "BTW" in name are VAT accounts
                // Real implementation would use proper account type classification
                if (line.Account?.Name.Contains("BTW Verkopen") == true)
                {
                    salesVAT += line.Credit - line.Debit;
                }
                else if (line.Account?.Name.Contains("BTW Inkopen") == true)
                {
                    purchaseVAT += line.Debit - line.Credit;
                }
            }
        }

        var netVAT = salesVAT - purchaseVAT;
        var calculationId = Guid.NewGuid();

        // Audit log for VAT calculation
        await _auditLog.LogAsync(tenantId, userId, "CALCULATE", "VATReturn", calculationId,
            new 
            { 
                Year = year, 
                Quarter = quarter, 
                PeriodStart = periodStart, 
                PeriodEnd = periodEnd,
                SalesVAT = salesVAT,
                PurchaseVAT = purchaseVAT,
                NetVAT = netVAT,
                Message = $"VAT calculated for Q{quarter} {year}: Net VAT €{netVAT:N2}"
            });

        return new VATCalculationResponse
        {
            CalculationId = calculationId,
            Year = year,
            Quarter = quarter,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            SalesVAT = salesVAT,
            PurchaseVAT = purchaseVAT,
            NetVAT = netVAT,
            CalculatedAt = DateTime.UtcNow,
            Status = "Calculated"
        };
    }

    public async Task<VATSubmissionResponse> SubmitVATAsync(Guid calculationId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        // In real implementation: submit to tax authority API
        var submissionId = Guid.NewGuid();
        var referenceNumber = $"BTW-{DateTime.UtcNow:yyyyMMdd}-{submissionId.ToString().Substring(0, 8).ToUpper()}";

        // Audit log for VAT submission
        await _auditLog.LogAsync(tenantId, userId, "SUBMIT", "VATReturn", calculationId,
            new 
            { 
                SubmissionId = submissionId,
                ReferenceNumber = referenceNumber,
                SubmittedAt = DateTime.UtcNow,
                Message = $"VAT return submitted to tax authority: {referenceNumber}"
            });

        return new VATSubmissionResponse
        {
            SubmissionId = submissionId,
            CalculationId = calculationId,
            ReferenceNumber = referenceNumber,
            SubmittedAt = DateTime.UtcNow,
            Status = "Submitted"
        };
    }

    public async Task<bool> ApproveVATAsync(Guid calculationId, string approvedBy, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        // Audit log for VAT approval
        await _auditLog.LogAsync(tenantId, userId, "APPROVE", "VATReturn", calculationId,
            new 
            { 
                ApprovedBy = approvedBy,
                ApprovedAt = DateTime.UtcNow,
                Message = $"VAT return approved by {approvedBy}"
            });

        return true;
    }
}
