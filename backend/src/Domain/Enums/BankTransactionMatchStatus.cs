namespace Boekhouding.Domain.Enums;

/// <summary>
/// Match status van een banktransactie
/// </summary>
public enum BankTransactionMatchStatus
{
    /// <summary>
    /// Nog niet gematched
    /// </summary>
    Unmatched = 0,
    
    /// <summary>
    /// Gematched met een factuur
    /// </summary>
    MatchedToInvoice = 1,
    
    /// <summary>
    /// Handmatig geboekt
    /// </summary>
    ManuallyBooked = 2,
    
    /// <summary>
    /// Geignoreerd
    /// </summary>
    Ignored = 3
}
