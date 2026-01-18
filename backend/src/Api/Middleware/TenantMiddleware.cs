using Boekhouding.Application.Interfaces;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Boekhouding.Api.Middleware;

/// <summary>
/// Middleware die de tenant context bepaalt op basis van de X-Tenant-Id header
/// en valideert of de gebruiker toegang heeft tot deze tenant
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private const string TenantIdHeaderName = "X-Tenant-Id";

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ApplicationDbContext dbContext,
        ILogger<TenantMiddleware> logger)
    {
        // Skip voor auth endpoints, health check en swagger
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (path.StartsWith("/api/auth") || 
            path.StartsWith("/health") || 
            path.StartsWith("/swagger") ||
            path.StartsWith("/api/tenants/my") ||
            path == "/api/tenants")
        {
            await _next(context);
            return;
        }

        // Check of header aanwezig is
        if (!context.Request.Headers.TryGetValue(TenantIdHeaderName, out var tenantIdHeader))
        {
            logger.LogWarning("Missing X-Tenant-Id header for path {Path}", path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Missing X-Tenant-Id header",
                message = "All requests must include a valid X-Tenant-Id header"
            });
            return;
        }

        // Valideer of header een geldige GUID is
        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            logger.LogWarning("Invalid X-Tenant-Id header: {TenantId}", tenantIdHeader.ToString());
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid X-Tenant-Id header",
                message = "X-Tenant-Id must be a valid GUID"
            });
            return;
        }

        // Check of gebruiker geauthenticeerd is
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            logger.LogWarning("User not authenticated for path {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "User must be authenticated"
            });
            return;
        }

        // Haal user ID op uit claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            logger.LogWarning("Invalid or missing user ID claim for path {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid user",
                message = "User ID claim is missing or invalid"
            });
            return;
        }

        // Valideer of gebruiker toegang heeft tot deze tenant
        var hasAccess = await dbContext.Set<Domain.Entities.UserTenant>()
            .AnyAsync(ut => ut.UserId == userId && ut.TenantId == tenantId);

        logger.LogInformation("Tenant access check: UserId={UserId}, TenantId={TenantId}, HasAccess={HasAccess}, Path={Path}", 
            userId, tenantId, hasAccess, path);

        if (!hasAccess)
        {
            logger.LogWarning("User {UserId} does not have access to tenant {TenantId}", userId, tenantId);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = $"User does not have access to tenant {tenantId}"
            });
            return;
        }

        // Tenant ID is geldig, set in context
        tenantContext.SetTenantId(tenantId);
        logger.LogDebug("Tenant context set: {TenantId} for path {Path}", tenantId, path);

        await _next(context);
    }
}
