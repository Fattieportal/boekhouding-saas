using Boekhouding.Domain.Common;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Bank transactie opgehaald via PSD2
/// </summary>
public class BankTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Referentie naar de bank connectie
    /// </summary>
    public Guid BankConnectionId { get; set; }
    
    /// <summary>
    /// Externe ID van de transactie bij de bank/provider (uniek)
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;
    
    /// <summary>
    /// Boekingsdatum
    /// </summary>
    public DateTime BookingDate { get; set; }
    
    /// <summary>
    /// Value date (valutadatum)
    /// </summary>
    public DateTime? ValueDate { get; set; }
    
    /// <summary>
    /// Bedrag (positief = credit, negatief = debit)
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Valuta
    /// </summary>
    public string Currency { get; set; } = "EUR";
    
    /// <summary>
    /// Naam van de tegenpartij
    /// </summary>
    public string? CounterpartyName { get; set; }
    
    /// <summary>
    /// IBAN van de tegenpartij
    /// </summary>
    public string? CounterpartyIban { get; set; }
    
    /// <summary>
    /// Omschrijving/beschrijving van de transactie
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Match status
    /// </summary>
    public BankTransactionMatchStatus MatchedStatus { get; set; } = BankTransactionMatchStatus.Unmatched;
    
    /// <summary>
    /// ID van de gematched factuur (indien van toepassing)
    /// </summary>
    public Guid? MatchedInvoiceId { get; set; }
    
    /// <summary>
    /// ID van de journal entry die is aangemaakt voor deze transactie
    /// </summary>
    public Guid? JournalEntryId { get; set; }
    
    /// <summary>
    /// Tijdstip waarop de transactie is gematched
    /// </summary>
    public DateTime? MatchedAt { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public BankConnection BankConnection { get; set; } = null!;
    public SalesInvoice? MatchedInvoice { get; set; }
    public JournalEntry? JournalEntry { get; set; }
}
