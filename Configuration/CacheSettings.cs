using System.ComponentModel.DataAnnotations;

namespace finaid.Configuration;

/// <summary>
/// Cache configuration settings
/// </summary>
public class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Cache provider type
    /// </summary>
    public CacheProvider Provider { get; set; } = CacheProvider.InMemory;

    /// <summary>
    /// Redis connection string (required if Provider is Redis)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Redis database number
    /// </summary>
    public int DefaultDatabase { get; set; } = 0;

    /// <summary>
    /// Key prefix for all cache keys
    /// </summary>
    public string KeyPrefix { get; set; } = "finaid";

    /// <summary>
    /// Default cache expiration time
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Redis-specific settings
    /// </summary>
    public RedisSettings Redis { get; set; } = new();

    /// <summary>
    /// In-memory cache settings
    /// </summary>
    public InMemorySettings InMemory { get; set; } = new();

    /// <summary>
    /// Cache policies for different data types
    /// </summary>
    public CachePolicies Policies { get; set; } = new();

    /// <summary>
    /// Circuit breaker settings for cache operations
    /// </summary>
    public CacheCircuitBreakerSettings CircuitBreaker { get; set; } = new();
}

/// <summary>
/// Cache provider types
/// </summary>
public enum CacheProvider
{
    /// <summary>
    /// In-memory caching (single instance)
    /// </summary>
    InMemory,

    /// <summary>
    /// Redis distributed cache
    /// </summary>
    Redis
}

/// <summary>
/// Redis-specific configuration
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Redis connection timeout in seconds
    /// </summary>
    public int ConnectTimeout { get; set; } = 5;

    /// <summary>
    /// Redis command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 5;

    /// <summary>
    /// Maximum number of connection retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Enable SSL/TLS for Redis connection
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Redis password (if required)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Allow administrator operations
    /// </summary>
    public bool AllowAdmin { get; set; } = false;

    /// <summary>
    /// Keep alive interval in seconds
    /// </summary>
    public int KeepAlive { get; set; } = 60;

    /// <summary>
    /// Connection pool size
    /// </summary>
    public int PoolSize { get; set; } = 10;
}

/// <summary>
/// In-memory cache configuration
/// </summary>
public class InMemorySettings
{
    /// <summary>
    /// Maximum memory size limit in MB
    /// </summary>
    public int SizeLimit { get; set; } = 100;

    /// <summary>
    /// Compact expired entries interval in minutes
    /// </summary>
    public int CompactionInterval { get; set; } = 30;

    /// <summary>
    /// Scan frequency for expired items in minutes
    /// </summary>
    public int ScanFrequency { get; set; } = 1;
}

/// <summary>
/// Cache policies for different types of data
/// </summary>
public class CachePolicies
{
    /// <summary>
    /// Eligibility check results cache duration
    /// </summary>
    public TimeSpan EligibilityResults { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Application status cache duration
    /// </summary>
    public TimeSpan ApplicationStatus { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// School codes cache duration
    /// </summary>
    public TimeSpan SchoolCodes { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Static reference data cache duration
    /// </summary>
    public TimeSpan StaticData { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// User profile data cache duration
    /// </summary>
    public TimeSpan UserProfile { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// API response cache duration for successful responses
    /// </summary>
    public TimeSpan ApiResponses { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Error response cache duration (to prevent repeated failures)
    /// </summary>
    public TimeSpan ErrorResponses { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Rate limit information cache duration
    /// </summary>
    public TimeSpan RateLimit { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Circuit breaker settings for cache operations
/// </summary>
public class CacheCircuitBreakerSettings
{
    /// <summary>
    /// Enable circuit breaker for cache operations
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Number of failures before opening circuit
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Time to keep circuit open before trying again
    /// </summary>
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Timeout for cache operations
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Exceptions that should be considered for circuit breaker
    /// </summary>
    public List<string> HandledExceptionTypes { get; set; } = new()
    {
        "StackExchange.Redis.RedisException",
        "StackExchange.Redis.RedisTimeoutException",
        "StackExchange.Redis.RedisConnectionException",
        "System.TimeoutException"
    };
}

/// <summary>
/// Cache key patterns for consistent naming
/// </summary>
public static class CacheKeys
{
    public const string EligibilityPrefix = "eligibility";
    public const string ApplicationPrefix = "application";
    public const string UserPrefix = "user";
    public const string SchoolPrefix = "school";
    public const string ApiResponsePrefix = "api_response";
    public const string RateLimitPrefix = "rate_limit";

    /// <summary>
    /// Generates cache key for eligibility results
    /// </summary>
    /// <param name="ssn">Student SSN (hashed)</param>
    /// <param name="year">Award year</param>
    /// <returns>Cache key</returns>
    public static string EligibilityKey(string ssn, int year)
        => $"{EligibilityPrefix}:{ssn}:{year}";

    /// <summary>
    /// Generates cache key for application status
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <returns>Cache key</returns>
    public static string ApplicationStatusKey(string applicationId)
        => $"{ApplicationPrefix}:status:{applicationId}";

    /// <summary>
    /// Generates cache key for user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Cache key</returns>
    public static string UserProfileKey(string userId)
        => $"{UserPrefix}:profile:{userId}";

    /// <summary>
    /// Generates cache key for school information
    /// </summary>
    /// <param name="schoolCode">Federal school code</param>
    /// <returns>Cache key</returns>
    public static string SchoolKey(string schoolCode)
        => $"{SchoolPrefix}:{schoolCode}";

    /// <summary>
    /// Generates cache key for API responses
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="parameters">Request parameters hash</param>
    /// <returns>Cache key</returns>
    public static string ApiResponseKey(string endpoint, string parameters)
        => $"{ApiResponsePrefix}:{endpoint}:{parameters}";

    /// <summary>
    /// Generates cache key for rate limit information
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>Cache key</returns>
    public static string RateLimitKey(string endpoint)
        => $"{RateLimitPrefix}:{endpoint}";
}