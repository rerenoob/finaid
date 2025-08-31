using System.ComponentModel.DataAnnotations;

namespace finaid.Configuration;

public class FederalApiSettings
{
    public const string SectionName = "FederalApi";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string? ClientId { get; set; }
    
    public string? ClientSecret { get; set; }

    public AuthenticationSettings Authentication { get; set; } = new();
    
    public RateLimitSettings RateLimit { get; set; } = new();
    
    public RetrySettings Retry { get; set; } = new();

    public EndpointSettings Endpoints { get; set; } = new();

    public MockSettings Mock { get; set; } = new();
}

public class AuthenticationSettings
{
    public string TokenEndpoint { get; set; } = "/oauth/token";
    
    public string AuthorizeEndpoint { get; set; } = "/oauth/authorize";
    
    public string Scope { get; set; } = "fafsa.read fafsa.write documents.upload";
    
    public int TokenExpirationBuffer { get; set; } = 300; // seconds
    
    public bool UseClientCredentials { get; set; } = true;
}

public class RateLimitSettings
{
    /// <summary>
    /// Maximum requests per hour for eligibility checks
    /// </summary>
    public int EligibilityPerHour { get; set; } = 500;
    
    /// <summary>
    /// Maximum applications submissions per day
    /// </summary>
    public int SubmissionsPerDay { get; set; } = 100;
    
    /// <summary>
    /// Maximum document uploads per hour
    /// </summary>
    public int DocumentsPerHour { get; set; } = 50;
    
    /// <summary>
    /// Maximum status checks per hour
    /// </summary>
    public int StatusChecksPerHour { get; set; } = 1000;
    
    /// <summary>
    /// Enable intelligent request queuing to respect rate limits
    /// </summary>
    public bool EnableQueuing { get; set; } = true;
    
    /// <summary>
    /// Request queue size limit
    /// </summary>
    public int QueueSizeLimit { get; set; } = 10000;
}

public class RetrySettings
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// Base delay between retries in milliseconds
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Maximum delay between retries in milliseconds
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;
    
    /// <summary>
    /// Use exponential backoff for retry delays
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
    
    /// <summary>
    /// HTTP status codes that should trigger retries
    /// </summary>
    public List<int> RetryableStatusCodes { get; set; } = new() { 429, 500, 502, 503, 504 };
}

public class EndpointSettings
{
    public string Eligibility { get; set; } = "/api/v1/eligibility";
    
    public string Applications { get; set; } = "/api/v1/applications";
    
    public string ApplicationStatus { get; set; } = "/api/v1/applications/{applicationId}/status";
    
    public string Documents { get; set; } = "/api/v1/applications/{applicationId}/documents";
    
    public string Verification { get; set; } = "/api/v1/verification";
    
    public string Awards { get; set; } = "/api/v1/awards";
    
    public string Corrections { get; set; } = "/api/v1/applications/{applicationId}/corrections";
    
    public string SchoolCodes { get; set; } = "/api/v1/schools";
}

public class MockSettings
{
    /// <summary>
    /// Enable mock API responses for development/testing
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Mock response delay in milliseconds to simulate network latency
    /// </summary>
    public int ResponseDelayMs { get; set; } = 500;
    
    /// <summary>
    /// Simulate random failures for testing resilience
    /// </summary>
    public bool SimulateFailures { get; set; } = false;
    
    /// <summary>
    /// Percentage chance of simulated failure (0-100)
    /// </summary>
    public int FailureRate { get; set; } = 10;
    
    /// <summary>
    /// Simulate rate limiting responses
    /// </summary>
    public bool SimulateRateLimit { get; set; } = false;
    
    /// <summary>
    /// Mock data file path for responses
    /// </summary>
    public string MockDataPath { get; set; } = "MockData/FederalApi";
}

/// <summary>
/// Federal API environment configuration
/// </summary>
public enum FederalApiEnvironment
{
    Development,
    Testing,
    Staging,
    Production
}

public class FederalApiEnvironmentSettings
{
    public FederalApiEnvironment Environment { get; set; } = FederalApiEnvironment.Development;
    
    public Dictionary<FederalApiEnvironment, FederalApiEnvironmentConfig> Configurations { get; set; } = new()
    {
        [FederalApiEnvironment.Development] = new()
        {
            BaseUrl = "https://api-dev.studentaid.gov",
            RequireHttps = false,
            EnableDetailedLogging = true,
            ValidateServerCertificate = false
        },
        [FederalApiEnvironment.Testing] = new()
        {
            BaseUrl = "https://api-test.studentaid.gov",
            RequireHttps = true,
            EnableDetailedLogging = true,
            ValidateServerCertificate = true
        },
        [FederalApiEnvironment.Staging] = new()
        {
            BaseUrl = "https://api-staging.studentaid.gov",
            RequireHttps = true,
            EnableDetailedLogging = false,
            ValidateServerCertificate = true
        },
        [FederalApiEnvironment.Production] = new()
        {
            BaseUrl = "https://api.studentaid.gov",
            RequireHttps = true,
            EnableDetailedLogging = false,
            ValidateServerCertificate = true
        }
    };
}

public class FederalApiEnvironmentConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    
    public bool RequireHttps { get; set; } = true;
    
    public bool EnableDetailedLogging { get; set; } = false;
    
    public bool ValidateServerCertificate { get; set; } = true;
    
    public int TimeoutSeconds { get; set; } = 30;
    
    public string UserAgent { get; set; } = "FinancialAidPlatform/1.0";
}