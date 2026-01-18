using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.DTOs.Reports;

/// <summary>
/// Profit & Loss (Winst & Verlies) rapport
/// </summary>
public class ProfitLossDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    // Revenue (Opbrengsten)
    public List<AccountLineDto> RevenueAccounts { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    
    // Expenses (Kosten)
    public List<AccountLineDto> ExpenseAccounts { get; set; } = new();
    public decimal TotalExpenses { get; set; }
    
    // Result
    public decimal NetIncome { get; set; } // TotalRevenue - TotalExpenses
}

/// <summary>
/// Balance Sheet (Balans) rapport
/// </summary>
public class BalanceSheetDto
{
    public DateTime AsOfDate { get; set; }
    
    // Assets (Activa)
    public List<AccountLineDto> AssetAccounts { get; set; } = new();
    public decimal TotalAssets { get; set; }
    
    // Liabilities (Passiva - Schulden)
    public List<AccountLineDto> LiabilityAccounts { get; set; } = new();
    public decimal TotalLiabilities { get; set; }
    
    // Equity (Eigen Vermogen)
    public List<AccountLineDto> EquityAccounts { get; set; } = new();
    public decimal TotalEquity { get; set; }
    
    // Balance check (should be 0)
    public decimal Balance { get; set; } // TotalAssets - (TotalLiabilities + TotalEquity)
}

/// <summary>
/// Regel in financieel rapport
/// </summary>
public class AccountLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
}
