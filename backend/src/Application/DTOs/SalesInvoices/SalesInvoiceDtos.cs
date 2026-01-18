using Boekhouding.Application.DTOs.Banking;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.DTOs.SalesInvoices;

public class SalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public decimal Subtotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal Total { get; set; }
    public decimal OpenAmount { get; set; }
    public bool IsUnpaid { get; set; }
    public bool IsOverdue { get; set; }
    public Guid? PdfFileId { get; set; }
    public Guid? TemplateId { get; set; }
    public string? Notes { get; set; }
    public Guid? JournalEntryId { get; set; }
    public List<SalesInvoiceLineDto> Lines { get; set; } = new();
    public List<PaymentTransactionDto> Payments { get; set; } = new(); // NEW: Matched bank transactions
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SalesInvoiceLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineVatAmount { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? AccountId { get; set; }
}

public class CreateSalesInvoiceDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public Guid ContactId { get; set; }
    public string Currency { get; set; } = "EUR";
    public Guid? TemplateId { get; set; }
    public string? Notes { get; set; }
    public List<CreateSalesInvoiceLineDto> Lines { get; set; } = new();
}

public class CreateSalesInvoiceLineDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public Guid? AccountId { get; set; }
}

public class UpdateSalesInvoiceDto
{
    public string? InvoiceNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? ContactId { get; set; }
    public string? Currency { get; set; }
    public Guid? TemplateId { get; set; }
    public string? Notes { get; set; }
    public List<CreateSalesInvoiceLineDto>? Lines { get; set; }
}
