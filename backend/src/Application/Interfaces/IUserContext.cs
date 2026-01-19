namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Context voor huidige gebruiker (read-only access)
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// ID van de huidige gebruiker
    /// </summary>
    Guid? UserId { get; }
    
    /// <summary>
    /// Email van de huidige gebruiker
    /// </summary>
    string? UserEmail { get; }
}
