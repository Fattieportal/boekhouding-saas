namespace Boekhouding.Domain.Enums;

/// <summary>
/// Rol van een gebruiker binnen een specifieke tenant
/// </summary>
public enum TenantRole
{
    Viewer = 0,
    Accountant = 1,
    Admin = 2
}
