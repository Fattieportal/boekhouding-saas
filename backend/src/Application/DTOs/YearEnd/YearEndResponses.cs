namespace Boekhouding.Application.DTOs.YearEnd;

public class YearEndCloseResponse
{
    public Guid ClosureId { get; set; }
    public int Year { get; set; }
    public DateTime ClosureDate { get; set; }
    public decimal NetIncome { get; set; }
    public Guid? ResultTransferEntryId { get; set; }
    public bool IsPermanent { get; set; }
    public string Status { get; set; } = "Closed";
}

public class OpeningBalancesResponse
{
    public Guid OpeningBalancesId { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? OpeningEntryId { get; set; }
    public int AccountsProcessed { get; set; }
    public string Status { get; set; } = "Created";
}
