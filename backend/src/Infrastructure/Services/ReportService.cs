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

    public ReportService(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<AccountsReceivableDto>> GetAccountsReceivableAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is not set");

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

        return grouped;
    }

    public async Task<VatReportDto> GetVatReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is not set");

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

        return report;
    }
}
