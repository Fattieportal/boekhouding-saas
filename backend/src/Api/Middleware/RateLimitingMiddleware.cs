using System.Collections.Concurrent;
using System.Net;

namespace Boekhouding.Api.Middleware;

/// <summary>
/// Middleware voor rate limiting op auth endpoints
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    // Store van IP + endpoint naar request timestamps
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _requestLog = new();
    
    // Rate limit configuratie
    private const int MaxRequestsPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    
    // Auth endpoints die rate limited moeten worden
    private static readonly HashSet<string> _rateLimitedEndpoints = new()
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh"
    };

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        // Check of dit een rate-limited endpoint is
        if (_rateLimitedEndpoints.Any(endpoint => path.StartsWith(endpoint.ToLowerInvariant())))
        {
            var clientId = GetClientIdentifier(context);
            var key = $"{clientId}:{path}";
            
            if (!IsRequestAllowed(key))
            {
                _logger.LogWarning("Rate limit exceeded for {ClientId} on {Path}", clientId, path);
                
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "Too many requests",
                    message = $"Rate limit exceeded. Maximum {MaxRequestsPerWindow} requests per {Window.TotalMinutes} minute(s).",
                    retryAfter = Window.TotalSeconds
                }));
                
                return;
            }
        }
        
        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Probeer eerst X-Forwarded-For header (voor reverse proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        // Gebruik remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsRequestAllowed(string key)
    {
        var now = DateTime.UtcNow;
        var requestQueue = _requestLog.GetOrAdd(key, _ => new Queue<DateTime>());
        
        lock (requestQueue)
        {
            // Verwijder oude requests buiten het window
            while (requestQueue.Count > 0 && requestQueue.Peek() < now - Window)
            {
                requestQueue.Dequeue();
            }
            
            // Check of limiet is bereikt
            if (requestQueue.Count >= MaxRequestsPerWindow)
            {
                return false;
            }
            
            // Voeg nieuwe request toe
            requestQueue.Enqueue(now);
            return true;
        }
    }
    
    // Cleanup oude entries (kan aangeroepen worden door een background service)
    public static void Cleanup()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _requestLog)
        {
            lock (kvp.Value)
            {
                while (kvp.Value.Count > 0 && kvp.Value.Peek() < now - Window)
                {
                    kvp.Value.Dequeue();
                }
                
                if (kvp.Value.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _requestLog.TryRemove(key, out _);
        }
    }
}
