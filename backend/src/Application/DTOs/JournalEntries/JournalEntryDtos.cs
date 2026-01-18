using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.DTOs.JournalEntries;

/// <summary>
/// DTO voor het aanmaken van een journaalpost (draft)
/// </summary>
public class CreateJournalEntryDto
{
    public Guid JournalId { get; set; }
    public DateTime EntryDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CreateJournalLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO voor het updaten van een journaalpost (alleen draft)
/// </summary>
public class UpdateJournalEntryDto
{
    public DateTime EntryDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CreateJournalLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO voor een journaalregel
/// </summary>
public class CreateJournalLineDto
{
    public Guid AccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

/// <summary>
/// DTO voor het ophalen van een journaalpost
/// </summary>
public class JournalEntryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid JournalId { get; set; }
    public string JournalCode { get; set; } = string.Empty;
    public string JournalName { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JournalEntryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? ReversalOfEntryId { get; set; }
    public List<JournalLineDto> Lines { get; set; } = new();
    
    // Berekende velden
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => TotalDebit == TotalCredit;
}

/// <summary>
/// DTO voor het ophalen van een journaalregel
/// </summary>
public class JournalLineDto
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

/// <summary>
/// Filter parameters voor journal entries
/// </summary>
public class JournalEntryFilterDto
{
    public Guid? JournalId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public JournalEntryStatus? Status { get; set; }
    public string? Reference { get; set; }
}
