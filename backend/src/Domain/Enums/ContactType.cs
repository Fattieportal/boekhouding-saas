namespace Boekhouding.Domain.Enums;

/// <summary>
/// Type contact: klant, leverancier of beide
/// </summary>
public enum ContactType
{
    /// <summary>
    /// Klant
    /// </summary>
    Customer = 1,
    
    /// <summary>
    /// Leverancier
    /// </summary>
    Supplier = 2,
    
    /// <summary>
    /// Zowel klant als leverancier
    /// </summary>
    Both = 3
}
