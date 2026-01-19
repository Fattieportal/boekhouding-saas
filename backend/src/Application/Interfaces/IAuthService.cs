using Boekhouding.Application.DTOs.Auth;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    Task LogoutAsync(Guid userId);
    Task<bool> UpdateUserRoleAsync(Guid userId, Role newRole, Guid performedByUserId);
}
