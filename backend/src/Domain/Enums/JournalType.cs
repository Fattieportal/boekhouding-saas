namespace Boekhouding.Domain.Enums;

/// <summary>
/// Type dagboek voor boekingen
/// </summary>
public enum JournalType
{
    /// <summary>
    /// Verkoopdagboek
    /// </summary>
    Sales = 1,
    
    /// <summary>
    /// Inkoopdagboek
    /// </summary>
    Purchase = 2,
    
    /// <summary>
    /// Bankdagboek
    /// </summary>
    Bank = 3,
    
    /// <summary>
    /// Memoriaal (diversen)
    /// </summary>
    General = 4
}
