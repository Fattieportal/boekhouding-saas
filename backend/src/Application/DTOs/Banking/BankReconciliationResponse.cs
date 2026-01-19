namespace Boekhouding.Application.DTOs.Banking;

/// <summary>
/// Response na bank reconciliatie
/// </summary>
public class BankReconciliationResponse
{
    public Guid ReconciliationId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public int TotalTransactions { get; set; }
    public int MatchedTransactions { get; set; }
    public int UnmatchedTransactions { get; set; }
    public decimal CalculatedBalance { get; set; }
    public decimal Difference { get; set; }
    public bool IsBalanced { get; set; }
    public DateTime ReconciledAt { get; set; }
}
