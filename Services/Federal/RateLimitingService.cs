using System.Collections.Concurrent;
using finaid.Configuration;
using Microsoft.Extensions.Options;

namespace finaid.Services.Federal;

public interface IRateLimitingService
{
    Task<bool> CanMakeRequestAsync(string endpoint, CancellationToken cancellationToken = default);
    Task RecordRequestAsync(string endpoint);
    Task<RateLimitStatus> GetRateLimitStatusAsync(string endpoint);
}

public class RateLimitStatus
{
    public string Endpoint { get; set; } = string.Empty;
    public int RequestsRemaining { get; set; }
    public int RequestLimit { get; set; }
    public DateTime ResetTime { get; set; }
    public TimeSpan TimeUntilReset => ResetTime - DateTime.UtcNow;
}

public class RateLimitingService : IRateLimitingService
{
    private readonly FederalApiConfiguration _configuration;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly ConcurrentDictionary<string, RequestCounter> _counters = new();

    public RateLimitingService(
        IOptions<FederalApiConfiguration> configuration,
        ILogger<RateLimitingService> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
    }

    public async Task<bool> CanMakeRequestAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var counter = GetOrCreateCounter(endpoint);
        await counter.CleanupExpiredRequestsAsync();

        var limit = GetEndpointLimit(endpoint);
        var currentCount = counter.GetCurrentCount();

        var canMakeRequest = currentCount < limit;
        
        if (!canMakeRequest)
        {
            _logger.LogWarning("Rate limit exceeded for endpoint {Endpoint} - {CurrentCount}/{Limit}",
                endpoint, currentCount, limit);
        }

        return canMakeRequest;
    }

    public async Task RecordRequestAsync(string endpoint)
    {
        var counter = GetOrCreateCounter(endpoint);
        await counter.RecordRequestAsync();

        _logger.LogDebug("Recorded request for endpoint {Endpoint} - Current count: {Count}",
            endpoint, counter.GetCurrentCount());
    }

    public async Task<RateLimitStatus> GetRateLimitStatusAsync(string endpoint)
    {
        var counter = GetOrCreateCounter(endpoint);
        await counter.CleanupExpiredRequestsAsync();

        var limit = GetEndpointLimit(endpoint);
        var currentCount = counter.GetCurrentCount();

        return new RateLimitStatus
        {
            Endpoint = endpoint,
            RequestsRemaining = Math.Max(0, limit - currentCount),
            RequestLimit = limit,
            ResetTime = counter.GetNextResetTime()
        };
    }

    private RequestCounter GetOrCreateCounter(string endpoint)
    {
        var key = NormalizeEndpoint(endpoint);
        return _counters.GetOrAdd(key, _ => new RequestCounter(GetTimeWindow(endpoint)));
    }

    private int GetEndpointLimit(string endpoint)
    {
        var normalized = NormalizeEndpoint(endpoint);
        
        return normalized switch
        {
            "eligibility" => _configuration.RateLimits.EligibilityApiRequestsPerHour,
            "applications" => _configuration.RateLimits.ApplicationSubmissionRequestsPerDay,
            "documents" => _configuration.RateLimits.DocumentUploadRequestsPerHour,
            "status" => _configuration.RateLimits.StatusCheckRequestsPerHour,
            _ => 100 // Default limit
        };
    }

    private TimeSpan GetTimeWindow(string endpoint)
    {
        var normalized = NormalizeEndpoint(endpoint);
        
        return normalized switch
        {
            "applications" => TimeSpan.FromDays(1), // Daily limit for applications
            _ => TimeSpan.FromHours(1) // Hourly limit for others
        };
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        var parts = endpoint.TrimStart('/').Split('/');
        return parts.Length > 0 ? parts[0].ToLowerInvariant() : endpoint;
    }
}

internal class RequestCounter
{
    private readonly TimeSpan _timeWindow;
    private readonly List<DateTime> _requests = new();
    private readonly object _lock = new();

    public RequestCounter(TimeSpan timeWindow)
    {
        _timeWindow = timeWindow;
    }

    public async Task RecordRequestAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _requests.Add(DateTime.UtcNow);
            }
        });
    }

    public async Task CleanupExpiredRequestsAsync()
    {
        await Task.Run(() =>
        {
            var cutoff = DateTime.UtcNow - _timeWindow;
            
            lock (_lock)
            {
                _requests.RemoveAll(request => request < cutoff);
            }
        });
    }

    public int GetCurrentCount()
    {
        lock (_lock)
        {
            return _requests.Count;
        }
    }

    public DateTime GetNextResetTime()
    {
        lock (_lock)
        {
            var oldest = _requests.FirstOrDefault();
            return oldest == default ? DateTime.UtcNow : oldest.Add(_timeWindow);
        }
    }
}