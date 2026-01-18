using Boekhouding.Application.DTOs.Reports;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Boekhouding.Infrastructure.Services;

public class FinancialReportService : IFinancialReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FinancialReportService> _logger;

    public FinancialReportService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        ILogger<FinancialReportService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<ProfitLossDto> GetProfitLossAsync(
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is not set");

        _logger.LogInformation("Generating P&L report for tenant {TenantId} from {From} to {To}", 
            tenantId, from, to);

        // Load all posted journal entries in period with their lines and accounts
        var entries = await _context.Set<JournalEntry>()
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .Where(e => e.TenantId == tenantId 
                     && e.Status == JournalEntryStatus.Posted
                     && e.EntryDate >= from 
                     && e.EntryDate <= to)
            .ToListAsync(cancellationToken);

        // Group lines by account (skip lines without account)
        var accountGroups = entries
            .SelectMany(e => e.Lines)
            .Where(l => l.Account != null)
            .GroupBy(l => l.Account!)
            .Select(g => new
            {
                Account = g.Key,
                Lines = g.ToList()
            })
            .ToList();

        var report = new ProfitLossDto
        {
            FromDate = from,
            ToDate = to
        };

        // Process Revenue accounts (AccountType = Revenue)
        var revenueGroups = accountGroups.Where(g => g.Account.Type == AccountType.Revenue);
        foreach (var group in revenueGroups)
        {
            var balance = CalculateBalance(group.Account.Type, group.Lines);
            report.RevenueAccounts.Add(new AccountLineDto
            {
                AccountId = group.Account.Id,
                AccountCode = group.Account.Code,
                AccountName = group.Account.Name,
                AccountType = group.Account.Type,
                Balance = balance,
                TransactionCount = group.Lines.Count
            });
        }
        report.TotalRevenue = report.RevenueAccounts.Sum(a => a.Balance);

        // Process Expense accounts (AccountType = Expense)
        var expenseGroups = accountGroups.Where(g => g.Account.Type == AccountType.Expense);
        foreach (var group in expenseGroups)
        {
            var balance = CalculateBalance(group.Account.Type, group.Lines);
            report.ExpenseAccounts.Add(new AccountLineDto
            {
                AccountId = group.Account.Id,
                AccountCode = group.Account.Code,
                AccountName = group.Account.Name,
                AccountType = group.Account.Type,
                Balance = balance,
                TransactionCount = group.Lines.Count
            });
        }
        report.TotalExpenses = report.ExpenseAccounts.Sum(a => a.Balance);

        // Net Income = Revenue - Expenses
        report.NetIncome = report.TotalRevenue - report.TotalExpenses;

        _logger.LogInformation("P&L report generated: Revenue={Revenue}, Expenses={Expenses}, NetIncome={NetIncome}",
            report.TotalRevenue, report.TotalExpenses, report.NetIncome);

        return report;
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(
        DateTime asOf, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is not set");

        _logger.LogInformation("Generating Balance Sheet for tenant {TenantId} as of {AsOf}", 
            tenantId, asOf);

        // Load all posted journal entries up to the date
        var entries = await _context.Set<JournalEntry>()
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .Where(e => e.TenantId == tenantId 
                     && e.Status == JournalEntryStatus.Posted
                     && e.EntryDate <= asOf)
            .ToListAsync(cancellationToken);

        // Group lines by account (skip lines without account)
        var accountGroups = entries
            .SelectMany(e => e.Lines)
            .Where(l => l.Account != null)
            .GroupBy(l => l.Account!)
            .Select(g => new
            {
                Account = g.Key,
                Lines = g.ToList()
            })
            .ToList();

        var report = new BalanceSheetDto
        {
            AsOfDate = asOf
        };

        // Process Asset accounts
        var assetGroups = accountGroups.Where(g => g.Account.Type == AccountType.Asset);
        foreach (var group in assetGroups)
        {
            var balance = CalculateBalance(group.Account.Type, group.Lines);
            report.AssetAccounts.Add(new AccountLineDto
            {
                AccountId = group.Account.Id,
                AccountCode = group.Account.Code,
                AccountName = group.Account.Name,
                AccountType = group.Account.Type,
                Balance = balance,
                TransactionCount = group.Lines.Count
            });
        }
        report.TotalAssets = report.AssetAccounts.Sum(a => a.Balance);

        // Process Liability accounts
        var liabilityGroups = accountGroups.Where(g => g.Account.Type == AccountType.Liability);
        foreach (var group in liabilityGroups)
        {
            var balance = CalculateBalance(group.Account.Type, group.Lines);
            report.LiabilityAccounts.Add(new AccountLineDto
            {
                AccountId = group.Account.Id,
                AccountCode = group.Account.Code,
                AccountName = group.Account.Name,
                AccountType = group.Account.Type,
                Balance = balance,
                TransactionCount = group.Lines.Count
            });
        }
        report.TotalLiabilities = report.LiabilityAccounts.Sum(a => a.Balance);

        // Process Equity accounts
        var equityGroups = accountGroups.Where(g => g.Account.Type == AccountType.Equity);
        foreach (var group in equityGroups)
        {
            var balance = CalculateBalance(group.Account.Type, group.Lines);
            report.EquityAccounts.Add(new AccountLineDto
            {
                AccountId = group.Account.Id,
                AccountCode = group.Account.Code,
                AccountName = group.Account.Name,
                AccountType = group.Account.Type,
                Balance = balance,
                TransactionCount = group.Lines.Count
            });
        }
        report.TotalEquity = report.EquityAccounts.Sum(a => a.Balance);

        // Balance check: Assets = Liabilities + Equity
        // If balanced, this should be 0
        report.Balance = report.TotalAssets - (report.TotalLiabilities + report.TotalEquity);

        _logger.LogInformation("Balance Sheet generated: Assets={Assets}, Liabilities={Liabilities}, Equity={Equity}, Balance={Balance}",
            report.TotalAssets, report.TotalLiabilities, report.TotalEquity, report.Balance);

        return report;
    }

    /// <summary>
    /// Calculate balance for an account based on its type
    /// </summary>
    private decimal CalculateBalance(AccountType accountType, List<JournalLine> lines)
    {
        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);

        // Normal balances:
        // - Asset, Expense: Debit balance (Debit - Credit)
        // - Liability, Equity, Revenue: Credit balance (Credit - Debit)
        return accountType switch
        {
            AccountType.Asset => totalDebit - totalCredit,
            AccountType.Liability => totalCredit - totalDebit,
            AccountType.Equity => totalCredit - totalDebit,
            AccountType.Revenue => totalCredit - totalDebit,
            AccountType.Expense => totalDebit - totalCredit,
            _ => 0
        };
    }
}
