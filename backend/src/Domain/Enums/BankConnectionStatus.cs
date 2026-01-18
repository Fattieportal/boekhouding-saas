namespace Boekhouding.Domain.Enums;

/// <summary>
/// Status van een bankkoppeling
/// </summary>
public enum BankConnectionStatus
{
    /// <summary>
    /// Connectie wordt opgezet
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Actief en bruikbaar
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Verlopen - hertoegang vereist
    /// </summary>
    Expired = 2,
    
    /// <summary>
    /// Gebruiker heeft toestemming ingetrokken
    /// </summary>
    Revoked = 3,
    
    /// <summary>
    /// Fout in de connectie
    /// </summary>
    Error = 4
}
