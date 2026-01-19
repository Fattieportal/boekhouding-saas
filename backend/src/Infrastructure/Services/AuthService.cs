using Boekhouding.Application.DTOs.Auth;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogService _auditLog;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        ApplicationDbContext context, 
        ITokenService tokenService,
        IAuditLogService auditLog,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _tokenService = tokenService;
        _auditLog = auditLog;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return null; // User already exists
        }

        // Parse role
        if (!Enum.TryParse<Role>(request.Role, true, out var role))
        {
            role = Role.Viewer; // Default to Viewer if invalid
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate token
        var token = _tokenService.GenerateToken(
            user.Id.ToString(), 
            user.Email, 
            user.Role.ToString());

        return new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // Find user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !user.IsActive)
        {
            // Log failed login attempt - user not found or inactive
            // For security: no tenant context on failed login, use Guid.Empty
            await LogFailedLoginAsync(request.Email, "User not found or inactive");
            return null; // User not found or inactive
        }

        // Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            // Log failed login attempt - wrong password
            await LogFailedLoginAsync(request.Email, "Invalid password");
            return null; // Invalid password
        }

        // Generate token
        var token = _tokenService.GenerateToken(
            user.Id.ToString(), 
            user.Email, 
            user.Role.ToString());

        // Log successful login
        await LogSuccessfulLoginAsync(user);

        return new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }

    private async Task LogSuccessfulLoginAsync(User user)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
        
        // For login, we don't have a tenant context yet, so we use Guid.Empty
        // The audit log system should handle this gracefully
        await _auditLog.LogAsync(
            Guid.Empty, // No tenant context during login
            user.Id, 
            "LOGIN", 
            "User", 
            user.Id,
            new 
            { 
                Email = user.Email, 
                Role = user.Role.ToString(), 
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Message = $"User {user.Email} logged in successfully"
            });
    }

    private async Task LogFailedLoginAsync(string email, string reason)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
        
        // For failed login, we also use Guid.Empty for tenant
        await _auditLog.LogAsync(
            Guid.Empty, 
            Guid.Empty, // No user ID for failed login
            "FAILED_LOGIN", 
            "User", 
            Guid.Empty,
            new 
            { 
                Email = email, 
                Reason = reason,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Message = $"Failed login attempt for {email}: {reason}"
            });
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        await _auditLog.LogAsync(
            Guid.Empty, // No tenant context during logout
            userId, 
            "LOGOUT", 
            "User", 
            userId,
            new 
            { 
                Email = user.Email, 
                IpAddress = ipAddress,
                Message = $"User {user.Email} logged out"
            });
    }

    public async Task<bool> UpdateUserRoleAsync(Guid userId, Role newRole, Guid performedByUserId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var oldRole = user.Role;
        if (oldRole == newRole) return true; // No change

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log permission change
        await _auditLog.LogAsync(
            Guid.Empty, // No tenant context for user management
            performedByUserId, 
            "PERMISSION_CHANGE", 
            "User", 
            userId,
            new 
            { 
                Email = user.Email,
                OldRole = oldRole.ToString(),
                NewRole = newRole.ToString(),
                Message = $"User {user.Email} role changed from {oldRole} to {newRole}"
            });

        return true;
    }
}
