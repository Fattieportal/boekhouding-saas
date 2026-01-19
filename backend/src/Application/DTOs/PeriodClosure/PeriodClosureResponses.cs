namespace Boekhouding.Application.DTOs.PeriodClosure;

public class PeriodClosureResponse
{
    public Guid ClosureId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime ClosedAt { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public int EntriesInPeriod { get; set; }
    public string Status { get; set; } = "Closed";
}

public class PeriodReopenResponse
{
    public Guid ReopenId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime ReopenedAt { get; set; }
    public string ReopenedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Reopened";
}
