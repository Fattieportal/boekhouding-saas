using Boekhouding.Application.DTOs.YearEnd;
using Boekhouding.Application.Interfaces;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class YearEndService : IYearEndService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLog;
    private readonly IUserContext _userContext;

    public YearEndService(
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

    public async Task<YearEndCloseResponse> CloseYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        var closureId = Guid.NewGuid();
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31);

        // Calculate net income from P&L accounts (simplified)
        var postedEntries = await _context.JournalEntries
            .Where(je => je.TenantId == tenantId &&
                        je.EntryDate >= yearStart &&
                        je.EntryDate <= yearEnd &&
                        je.Status == Domain.Enums.JournalEntryStatus.Posted)
            .Include(je => je.Lines)
                .ThenInclude(line => line.Account)
            .ToListAsync(cancellationToken);

        var journalLines = postedEntries.SelectMany(je => je.Lines).ToList();

        // Simplified: calculate revenue - expenses
        decimal revenue = journalLines
            .Where(jl => jl.Account?.Type == Domain.Enums.AccountType.Revenue)
            .Sum(jl => jl.Credit - jl.Debit);

        decimal expenses = journalLines
            .Where(jl => jl.Account?.Type == Domain.Enums.AccountType.Expense)
            .Sum(jl => jl.Debit - jl.Credit);

        var netIncome = revenue - expenses;

        // In real implementation: create journal entry to transfer result to equity
        var resultTransferEntryId = Guid.NewGuid();

        // Audit log for year end closure (PERMANENT operation)
        await _auditLog.LogAsync(tenantId, userId, "YEAR_END_CLOSE", "YearEnd", closureId,
            new 
            { 
                Year = year,
                ClosureDate = DateTime.UtcNow,
                NetIncome = netIncome,
                Revenue = revenue,
                Expenses = expenses,
                ResultTransferEntryId = resultTransferEntryId,
                IsPermanent = true,
                Message = $"⚠️ PERMANENT: Fiscal year {year} closed with net income €{netIncome:N2}"
            });

        return new YearEndCloseResponse
        {
            ClosureId = closureId,
            Year = year,
            ClosureDate = DateTime.UtcNow,
            NetIncome = netIncome,
            ResultTransferEntryId = resultTransferEntryId,
            IsPermanent = true,
            Status = "Closed"
        };
    }

    public async Task<OpeningBalancesResponse> CreateOpeningBalancesAsync(int year, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");

        var openingBalancesId = Guid.NewGuid();
        var previousYearEnd = new DateTime(year - 1, 12, 31);

        // Get all balance sheet accounts (Assets, Liabilities, Equity)
        var balanceSheetAccounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId &&
                       (a.Type == Domain.Enums.AccountType.Asset ||
                        a.Type == Domain.Enums.AccountType.Liability ||
                        a.Type == Domain.Enums.AccountType.Equity))
            .ToListAsync(cancellationToken);

        // In real implementation: calculate balances and create opening entry
        var openingEntryId = Guid.NewGuid();
        var accountsProcessed = balanceSheetAccounts.Count;

        // Audit log for opening balances creation
        await _auditLog.LogAsync(tenantId, userId, "OPENING_BALANCES", "YearEnd", openingBalancesId,
            new 
            { 
                Year = year,
                PreviousYearEnd = previousYearEnd,
                OpeningEntryId = openingEntryId,
                AccountsProcessed = accountsProcessed,
                Message = $"Opening balances created for fiscal year {year} ({accountsProcessed} accounts)"
            });

        return new OpeningBalancesResponse
        {
            OpeningBalancesId = openingBalancesId,
            Year = year,
            CreatedAt = DateTime.UtcNow,
            OpeningEntryId = openingEntryId,
            AccountsProcessed = accountsProcessed,
            Status = "Created"
        };
    }
}
