namespace Boekhouding.Domain.Enums;

/// <summary>
/// Type grootboekrekening volgens dubbel boekhouden
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Activa (bezittingen)
    /// </summary>
    Asset = 1,
    
    /// <summary>
    /// Passiva (schulden)
    /// </summary>
    Liability = 2,
    
    /// <summary>
    /// Eigen vermogen
    /// </summary>
    Equity = 3,
    
    /// <summary>
    /// Opbrengsten/Omzet
    /// </summary>
    Revenue = 4,
    
    /// <summary>
    /// Kosten
    /// </summary>
    Expense = 5
}
