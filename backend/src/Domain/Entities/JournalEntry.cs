using Boekhouding.Domain.Common;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Journaalpost - een boeking in het grootboek
/// </summary>
public class JournalEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Dagboek waartoe deze post behoort
    /// </summary>
    public Guid JournalId { get; set; }
    
    /// <summary>
    /// Datum van de boeking
    /// </summary>
    public DateTime EntryDate { get; set; }
    
    /// <summary>
    /// Referentie (bijv. factuurnummer, betalingsnummer)
    /// </summary>
    public string Reference { get; set; } = string.Empty;
    
    /// <summary>
    /// Omschrijving van de boeking
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Status: Draft/Posted/Reversed
    /// </summary>
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    
    /// <summary>
    /// Tijdstip waarop de post is geboekt
    /// </summary>
    public DateTime? PostedAt { get; set; }
    
    /// <summary>
    /// ID van de originele entry als dit een reversal is
    /// </summary>
    public Guid? ReversalOfEntryId { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public Journal Journal { get; set; } = null!;
    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
    
    /// <summary>
    /// Als dit een reversal is, de originele entry
    /// </summary>
    public JournalEntry? ReversalOfEntry { get; set; }
    
    /// <summary>
    /// Als deze entry is teruggedraaid, de reversal entry
    /// </summary>
    public JournalEntry? ReversedByEntry { get; set; }
}
