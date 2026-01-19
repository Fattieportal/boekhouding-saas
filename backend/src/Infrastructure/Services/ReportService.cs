using Boekhouding.Application.DTOs.Reports;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLog;
    private readonly IUserContext _userContext;

    public ReportService(
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

    public async Task<List<AccountsReceivableDto>> GetAccountsReceivableAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        // Get all posted invoices that are not yet paid
        var outstandingInvoices = await _context.Set<SalesInvoice>()
            .Include(i => i.Contact)
            .Where(i => i.Status == InvoiceStatus.Posted)
            .OrderBy(i => i.Contact!.DisplayName)
            .ThenBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

        // Group by contact
        var grouped = outstandingInvoices
            .GroupBy(i => new { i.ContactId, ContactName = i.Contact!.DisplayName })
            .Select(g => new AccountsReceivableDto
            {
                ContactId = g.Key.ContactId,
                ContactName = g.Key.ContactName,
                TotalOutstanding = g.Sum(i => i.Total),
                InvoiceCount = g.Count(),
                Invoices = g.Select(i => new OutstandingInvoiceDto
                {
                    InvoiceId = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    IssueDate = i.IssueDate,
                    DueDate = i.DueDate,
                    Total = i.Total,
                    Outstanding = i.Total, // In future, subtract payments
                    DaysOverdue = i.DueDate < DateTime.UtcNow.Date 
                        ? (int)(DateTime.UtcNow.Date - i.DueDate).TotalDays 
                        : 0
                }).ToList()
            })
            .OrderByDescending(ar => ar.TotalOutstanding)
            .ToList();

        // Audit log for report generation
        var totalOutstanding = grouped.Sum(g => g.TotalOutstanding);
        await _auditLog.LogAsync(tenantId, userId, "GENERATE_REPORT", "Report", Guid.NewGuid(),
            new 
            { 
                ReportType = "AccountsReceivable",
                ContactCount = grouped.Count,
                TotalOutstanding = totalOutstanding,
                TotalInvoices = grouped.Sum(g => g.InvoiceCount),
                Message = $"Accounts Receivable report generated: {grouped.Count} contacts, €{totalOutstanding:N2} outstanding"
            });

        return grouped;
    }

    public async Task<VatReportDto> GetVatReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        // Ensure dates are UTC
        var fromDateUtc = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        var toDateUtc = DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc); // End of day

        // Get all posted invoices within the period
        var postedInvoices = await _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .Where(i => i.TenantId == tenantId 
                && (i.Status == InvoiceStatus.Posted || i.Status == InvoiceStatus.Paid)
                && i.IssueDate >= fromDateUtc
                && i.IssueDate <= toDateUtc)
            .ToListAsync(cancellationToken);

        // Extract all invoice lines
        var allLines = postedInvoices
            .SelectMany(i => i.Lines)
            .ToList();

        // Group by VAT rate
        var vatBreakdown = allLines
            .GroupBy(line => line.VatRate)
            .Select(group => new VatRateBreakdownDto
            {
                VatRate = group.Key,
                Revenue = group.Sum(line => line.LineSubtotal),
                VatAmount = group.Sum(line => line.LineVatAmount),
                LineCount = group.Count()
            })
            .OrderBy(x => x.VatRate)
            .ToList();

        var report = new VatReportDto
        {
            FromDate = fromDateUtc,
            ToDate = toDateUtc,
            VatRates = vatBreakdown,
            TotalRevenue = vatBreakdown.Sum(x => x.Revenue),
            TotalVat = vatBreakdown.Sum(x => x.VatAmount),
            TotalIncludingVat = vatBreakdown.Sum(x => x.Revenue + x.VatAmount),
            InvoiceCount = postedInvoices.Count
        };

        // Audit log for VAT report generation
        await _auditLog.LogAsync(tenantId, userId, "GENERATE_REPORT", "Report", Guid.NewGuid(),
            new 
            { 
                ReportType = "VATReport",
                FromDate = fromDateUtc,
                ToDate = toDateUtc,
                TotalRevenue = report.TotalRevenue,
                TotalVAT = report.TotalVat,
                InvoiceCount = report.InvoiceCount,
                Message = $"VAT report generated for period {fromDateUtc:yyyy-MM-dd} to {toDateUtc:yyyy-MM-dd}: €{report.TotalVat:N2} VAT"
            });

        return report;
    }

    public async Task<Guid> ExportReportToPdfAsync(string reportType, object reportData, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        var exportId = Guid.NewGuid();

        // In real implementation: generate PDF using IPdfRenderer
        // For now, just log the export

        await _auditLog.LogAsync(tenantId, userId, "EXPORT_PDF", "Report", exportId,
            new 
            { 
                ReportType = reportType,
                ExportId = exportId,
                ExportedAt = DateTime.UtcNow,
                Message = $"Report '{reportType}' exported to PDF"
            });

        return exportId;
    }

    public async Task<Guid> ExportReportToExcelAsync(string reportType, object reportData, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        var exportId = Guid.NewGuid();

        // In real implementation: generate Excel file
        // For now, just log the export

        await _auditLog.LogAsync(tenantId, userId, "EXPORT_EXCEL", "Report", exportId,
            new 
            { 
                ReportType = reportType,
                ExportId = exportId,
                ExportedAt = DateTime.UtcNow,
                Message = $"Report '{reportType}' exported to Excel"
            });

        return exportId;
    }
}
