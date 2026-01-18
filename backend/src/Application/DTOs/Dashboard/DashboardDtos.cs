namespace Boekhouding.Application.DTOs.Dashboard;

public class DashboardDto
{
    public InvoiceStatsDto Invoices { get; set; } = new();
    public RevenueStatsDto Revenue { get; set; } = new();
    public BankStatsDto Bank { get; set; } = new();
    public List<RecentActivityDto> Activity { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
}

public class InvoiceStatsDto
{
    public int UnpaidCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OpenAmountTotal { get; set; }
    public decimal PaidThisPeriodAmount { get; set; }
    public int PaidThisPeriodCount { get; set; }
}

public class RevenueStatsDto
{
    public decimal RevenueExclThisPeriod { get; set; }
    public decimal VatThisPeriod { get; set; }
    public decimal RevenueInclThisPeriod { get; set; }
}

public class BankStatsDto
{
    public DateTime? LastSyncAt { get; set; }
    public int UnmatchedTransactionsCount { get; set; }
    public int MatchedTransactionsCount { get; set; }
}

public class RecentActivityDto
{
    public DateTime Timestamp { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class TopCustomerDto
{
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int InvoiceCount { get; set; }
}
