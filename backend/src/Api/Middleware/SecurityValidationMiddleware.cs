using System.Net;

namespace Boekhouding.Api.Middleware;

/// <summary>
/// Middleware voor security validatie van headers en tenant isolation
/// </summary>
public class SecurityValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityValidationMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly HashSet<string> _allowedOrigins;

    public SecurityValidationMiddleware(RequestDelegate next, ILogger<SecurityValidationMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        
        // Load allowed origins from configuration (same as CORS)
        var configuredOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000", "https://localhost:3000" };
        _allowedOrigins = new HashSet<string>(configuredOrigins);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Valideer Origin header voor API calls (niet voor health check)
        if (!context.Request.Path.StartsWithSegments("/health") && 
            !context.Request.Path.StartsWithSegments("/swagger"))
        {
            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            
            // Als er een Origin header is (CORS request), valideer deze
            if (!string.IsNullOrEmpty(origin))
            {
                // Check if wildcard is allowed OR origin is in the allowed list
                if (!_allowedOrigins.Contains("*") && !_allowedOrigins.Contains(origin))
                {
                    _logger.LogWarning("Request blocked from unauthorized origin: {Origin}", origin);
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await context.Response.WriteAsync("Forbidden: Invalid origin");
                    return;
                }
            }
            
            // Valideer dat authenticated requests een X-Tenant-Id header hebben
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var tenantHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var tenantIdClaim = context.User.Claims
                    .FirstOrDefault(c => c.Type == "TenantId")?.Value;
                
                // Voor endpoints die tenant-specific zijn
                if (!string.IsNullOrEmpty(tenantIdClaim))
                {
                    // X-Tenant-Id moet aanwezig zijn en matchen met de claim
                    if (string.IsNullOrEmpty(tenantHeader))
                    {
                        _logger.LogWarning("Missing X-Tenant-Id header for authenticated request by user {UserId}", 
                            context.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                        
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync("Missing X-Tenant-Id header");
                        return;
                    }
                    
                    if (!Guid.TryParse(tenantHeader, out var headerTenantId) || 
                        !Guid.TryParse(tenantIdClaim, out var claimTenantId) ||
                        headerTenantId != claimTenantId)
                    {
                        _logger.LogWarning(
                            "Tenant isolation violation: Header {HeaderTenant} does not match claim {ClaimTenant} for user {UserId}",
                            tenantHeader,
                            tenantIdClaim,
                            context.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                        
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        await context.Response.WriteAsync("Forbidden: Tenant mismatch");
                        return;
                    }
                }
            }
            
            // Valideer Content-Type voor POST/PUT requests
            if ((context.Request.Method == "POST" || context.Request.Method == "PUT") &&
                context.Request.ContentLength > 0)
            {
                var contentType = context.Request.ContentType?.ToLowerInvariant() ?? "";
                
                if (!contentType.StartsWith("application/json") && 
                    !contentType.StartsWith("multipart/form-data"))
                {
                    _logger.LogWarning("Invalid Content-Type: {ContentType}", contentType);
                    context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                    await context.Response.WriteAsync("Unsupported Media Type");
                    return;
                }
            }
        }
        
        await _next(context);
    }
}
