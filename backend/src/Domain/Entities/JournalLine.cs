using Boekhouding.Domain.Common;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Regel binnen een journaalpost (debet of credit)
/// </summary>
public class JournalLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Journaalpost waartoe deze regel behoort
    /// </summary>
    public Guid EntryId { get; set; }
    
    /// <summary>
    /// Grootboekrekening
    /// </summary>
    public Guid AccountId { get; set; }
    
    /// <summary>
    /// Omschrijving van deze regel
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Debet bedrag (altijd >= 0)
    /// </summary>
    public decimal Debit { get; set; }
    
    /// <summary>
    /// Credit bedrag (altijd >= 0)
    /// </summary>
    public decimal Credit { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public JournalEntry Entry { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
