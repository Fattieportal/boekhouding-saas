namespace Boekhouding.Domain.Enums;

/// <summary>
/// Status van een sales invoice
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// Concept - nog niet verzonden
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Verzonden naar klant
    /// </summary>
    Sent = 1,
    
    /// <summary>
    /// Geboekt in de administratie
    /// </summary>
    Posted = 2,
    
    /// <summary>
    /// Betaald
    /// </summary>
    Paid = 3
}
