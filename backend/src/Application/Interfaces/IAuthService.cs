using Boekhouding.Application.DTOs.Auth;

namespace Boekhouding.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
