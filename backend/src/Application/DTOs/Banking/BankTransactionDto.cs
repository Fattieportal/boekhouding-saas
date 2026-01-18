using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.DTOs.Banking;

/// <summary>
/// Bank transactie van provider (for sync responses)
/// </summary>
public class BankTransactionDto
{
    public string ExternalId { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? CounterpartyName { get; set; }
    public string? CounterpartyIban { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Volledige bank transactie met matching status (for GET requests)
/// </summary>
public class BankTransactionFullDto
{
    public Guid Id { get; set; }
    public Guid BankConnectionId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? CounterpartyName { get; set; }
    public string? CounterpartyIban { get; set; }
    public string? Description { get; set; }
    public BankTransactionMatchStatus MatchedStatus { get; set; }
    public Guid? MatchedInvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? JournalEntryId { get; set; }
    public DateTime? MatchedAt { get; set; }
}

/// <summary>
/// Payment info voor gebruik in SalesInvoiceDto
/// </summary>
public class PaymentTransactionDto
{
    public Guid Id { get; set; }
    public DateTime BookingDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? CounterpartyName { get; set; }
    public string? Description { get; set; }
    public Guid? JournalEntryId { get; set; }
    public DateTime MatchedAt { get; set; }
}
