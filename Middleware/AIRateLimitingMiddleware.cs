using finaid.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net;

namespace finaid.Middleware;

/// <summary>
/// Middleware to enforce rate limiting for AI API requests
/// </summary>
public class AIRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AzureOpenAISettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIRateLimitingMiddleware> _logger;

    public AIRateLimitingMiddleware(
        RequestDelegate next,
        IOptions<AzureOpenAISettings> settings,
        IMemoryCache cache,
        ILogger<AIRateLimitingMiddleware> logger)
    {
        _next = next;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply rate limiting to AI-related endpoints
        if (!IsAIEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var isRateLimited = await CheckRateLimitAsync(clientId);

        if (isRateLimited)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on path {Path}", 
                clientId, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Add("Retry-After", "60");
            
            await context.Response.WriteAsync(
                "Rate limit exceeded. Please wait before making additional AI requests.");
            return;
        }

        // Record the request
        await RecordRequestAsync(clientId);

        await _next(context);
    }

    private static bool IsAIEndpoint(PathString path)
    {
        var aiPaths = new[] 
        { 
            "/api/ai/", 
            "/api/chat/", 
            "/api/assistant/",
            "/hubs/chat" 
        };

        return aiPaths.Any(aiPath => path.Value?.Contains(aiPath, StringComparison.OrdinalIgnoreCase) == true);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get user ID from claims
        var userId = context.User?.FindFirst("sub")?.Value 
                    ?? context.User?.FindFirst("id")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    private async Task<bool> CheckRateLimitAsync(string clientId)
    {
        var currentMinute = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm");
        var requestCountKey = $"ai_requests:{clientId}:{currentMinute}";
        var tokenCountKey = $"ai_tokens:{clientId}:{currentMinute}";

        var requestCount = await GetCountFromCacheAsync(requestCountKey);
        var tokenCount = await GetCountFromCacheAsync(tokenCountKey);

        // Check request rate limit
        if (requestCount >= _settings.RequestsPerMinute)
        {
            return true;
        }

        // Check token rate limit (approximated based on average request size)
        var estimatedTokensPerRequest = 1000; // Conservative estimate
        if (tokenCount + estimatedTokensPerRequest >= _settings.TokensPerMinute)
        {
            return true;
        }

        return false;
    }

    private async Task RecordRequestAsync(string clientId)
    {
        var currentMinute = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm");
        var requestCountKey = $"ai_requests:{clientId}:{currentMinute}";

        var requestCount = await GetCountFromCacheAsync(requestCountKey);
        
        var cacheOptions = new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };

        _cache.Set(requestCountKey, requestCount + 1, cacheOptions);
    }

    private async Task<int> GetCountFromCacheAsync(string key)
    {
        return await Task.FromResult(_cache.GetOrCreate(key, factory => 0));
    }
}

/// <summary>
/// Extension method to register the AI rate limiting middleware
/// </summary>
public static class AIRateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseAIRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AIRateLimitingMiddleware>();
    }
}