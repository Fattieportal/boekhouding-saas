namespace Boekhouding.Domain.Enums;

/// <summary>
/// Status van een journaalpost
/// </summary>
public enum JournalEntryStatus
{
    /// <summary>
    /// Concept - kan nog worden aangepast
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Geboekt - immutable, onderdeel van de grootboekadministratie
    /// </summary>
    Posted = 1,
    
    /// <summary>
    /// Teruggedraaid via een reversal entry
    /// </summary>
    Reversed = 2
}
