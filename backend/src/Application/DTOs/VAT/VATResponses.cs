namespace Boekhouding.Application.DTOs.VAT;

public class VATCalculationResponse
{
    public Guid CalculationId { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal SalesVAT { get; set; }
    public decimal PurchaseVAT { get; set; }
    public decimal NetVAT { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string Status { get; set; } = "Calculated";
}

public class VATSubmissionResponse
{
    public Guid SubmissionId { get; set; }
    public Guid CalculationId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = "Submitted";
}
