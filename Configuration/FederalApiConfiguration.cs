using System.ComponentModel.DataAnnotations;

namespace finaid.Configuration;

public class FederalApiConfiguration
{
    public const string SectionName = "FederalApi";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://api.studentaid.gov";

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    [Required]
    [Url]
    public string AuthorizationUrl { get; set; } = "https://api.studentaid.gov/oauth/authorize";

    [Required]
    [Url]
    public string TokenUrl { get; set; } = "https://api.studentaid.gov/oauth/token";

    [Required]
    public string Scope { get; set; } = "fafsa.read fafsa.write documents.upload";

    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerTimeoutSeconds { get; set; } = 60;

    public RateLimitConfiguration RateLimits { get; set; } = new();

    public bool UseMockService { get; set; } = true; // Default to mock until real API access
}

public class RateLimitConfiguration
{
    public int EligibilityApiRequestsPerHour { get; set; } = 500;
    public int ApplicationSubmissionRequestsPerDay { get; set; } = 100;
    public int DocumentUploadRequestsPerHour { get; set; } = 50;
    public int StatusCheckRequestsPerHour { get; set; } = 1000;
}