namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Service voor het ophalen van de huidige gebruiker
/// </summary>
public interface ICurrentUserService
{
    Guid? GetUserId();
    string? GetUserEmail();
}
