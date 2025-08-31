using System.Net;
using finaid.Models.Federal;

namespace finaid.Services.Federal;

/// <summary>
/// Mock implementation of Federal API client for development and testing
/// </summary>
public class MockFederalApiService : IFederalApiClient
{
    private readonly ILogger<MockFederalApiService> _logger;
    private readonly Random _random = new();
    private bool _isAuthenticated = false;

    public MockFederalApiService(ILogger<MockFederalApiService> logger)
    {
        _logger = logger;
    }

    public bool IsAuthenticated => _isAuthenticated;

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        await SimulateNetworkDelay();
        
        _logger.LogDebug("Mock GET request to {Endpoint}", endpoint);

        if (!_isAuthenticated && !endpoint.Contains("health"))
        {
            return ApiResponse<T>.Error("Not authenticated", HttpStatusCode.Unauthorized);
        }

        // Simulate random failures (5% failure rate)
        if (ShouldSimulateFailure())
        {
            return ApiResponse<T>.Error("Simulated API failure", HttpStatusCode.ServiceUnavailable);
        }

        var mockData = GenerateMockResponse<T>(endpoint);
        return ApiResponse<T>.Success(mockData);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? payload = null, CancellationToken cancellationToken = default)
    {
        await SimulateNetworkDelay();
        
        _logger.LogDebug("Mock POST request to {Endpoint}", endpoint);

        if (!_isAuthenticated)
        {
            return ApiResponse<T>.Error("Not authenticated", HttpStatusCode.Unauthorized);
        }

        if (ShouldSimulateFailure())
        {
            return ApiResponse<T>.Error("Simulated API failure", HttpStatusCode.ServiceUnavailable);
        }

        var mockData = GenerateMockResponse<T>(endpoint);
        return ApiResponse<T>.Success(mockData, HttpStatusCode.Created);
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? payload = null, CancellationToken cancellationToken = default)
    {
        await SimulateNetworkDelay();
        
        _logger.LogDebug("Mock PUT request to {Endpoint}", endpoint);

        if (!_isAuthenticated)
        {
            return ApiResponse<T>.Error("Not authenticated", HttpStatusCode.Unauthorized);
        }

        if (ShouldSimulateFailure())
        {
            return ApiResponse<T>.Error("Simulated API failure", HttpStatusCode.ServiceUnavailable);
        }

        var mockData = GenerateMockResponse<T>(endpoint);
        return ApiResponse<T>.Success(mockData);
    }

    public async Task<ApiResponse<T>> UploadFileAsync<T>(
        string endpoint,
        Stream fileContent,
        string fileName,
        string contentType,
        Dictionary<string, string>? additionalFields = null,
        CancellationToken cancellationToken = default)
    {
        await SimulateNetworkDelay(1000, 3000); // File uploads take longer
        
        _logger.LogDebug("Mock file upload to {Endpoint} - File: {FileName}", endpoint, fileName);

        if (!_isAuthenticated)
        {
            return ApiResponse<T>.Error("Not authenticated", HttpStatusCode.Unauthorized);
        }

        if (ShouldSimulateFailure())
        {
            return ApiResponse<T>.Error("Simulated upload failure", HttpStatusCode.ServiceUnavailable);
        }

        var mockData = GenerateMockFileUploadResponse<T>(fileName, fileContent.Length);
        return ApiResponse<T>.Success(mockData, HttpStatusCode.Created);
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        await SimulateNetworkDelay(200, 500);
        
        _logger.LogDebug("Mock connection validation");

        // Simulate occasional connection issues
        if (_random.NextDouble() < 0.1) // 10% failure rate
        {
            return false;
        }

        // Auto-authenticate on successful connection validation
        _isAuthenticated = true;
        return true;
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await SimulateNetworkDelay(300, 800);
        
        _logger.LogDebug("Mock token refresh");

        // Simulate occasional refresh failures
        if (_random.NextDouble() < 0.05) // 5% failure rate
        {
            _isAuthenticated = false;
            return false;
        }

        _isAuthenticated = true;
        return true;
    }

    private async Task SimulateNetworkDelay(int minMs = 100, int maxMs = 1000)
    {
        var delay = _random.Next(minMs, maxMs);
        await Task.Delay(delay);
    }

    private bool ShouldSimulateFailure()
    {
        return _random.NextDouble() < 0.05; // 5% failure rate
    }

    private T GenerateMockResponse<T>(string endpoint)
    {
        var responseType = typeof(T);

        // Handle specific endpoint responses
        if (endpoint.Contains("health"))
        {
            var healthResponse = new { status = "healthy", timestamp = DateTime.UtcNow };
            return (T)(object)healthResponse;
        }

        if (endpoint.Contains("eligibility"))
        {
            var eligibilityResponse = new
            {
                studentId = Guid.NewGuid().ToString(),
                eligibilityStatus = "eligible",
                citizenshipStatus = "citizen",
                dependencyStatus = "dependent",
                priorYearIncome = _random.Next(20000, 100000),
                expectedFamilyContribution = _random.Next(1000, 15000),
                pellGrant = new
                {
                    eligible = true,
                    maxAward = 7395,
                    estimatedAward = _random.Next(2000, 7395)
                }
            };
            return (T)(object)eligibilityResponse;
        }

        if (endpoint.Contains("applications"))
        {
            var applicationResponse = new
            {
                applicationId = Guid.NewGuid().ToString(),
                confirmationNumber = $"FAFSA{_random.Next(100000, 999999)}",
                status = "submitted",
                estimatedProcessingTime = "3-5 business days",
                submittedAt = DateTime.UtcNow
            };
            return (T)(object)applicationResponse;
        }

        if (endpoint.Contains("status"))
        {
            var statuses = new[] { "processing", "complete", "requires_correction", "rejected" };
            var statusResponse = new
            {
                applicationId = Guid.NewGuid().ToString(),
                status = statuses[_random.Next(statuses.Length)],
                lastUpdated = DateTime.UtcNow.AddDays(-_random.Next(1, 7)),
                isirAvailable = _random.NextDouble() > 0.5,
                verificationRequired = _random.NextDouble() > 0.7
            };
            return (T)(object)statusResponse;
        }

        // Default response for unknown endpoints
        var defaultResponse = new
        {
            success = true,
            message = $"Mock response for {endpoint}",
            timestamp = DateTime.UtcNow,
            data = new { mockData = true }
        };

        return (T)(object)defaultResponse;
    }

    private T GenerateMockFileUploadResponse<T>(string fileName, long fileSize)
    {
        var uploadResponse = new
        {
            documentId = Guid.NewGuid().ToString(),
            fileName = fileName,
            fileSize = fileSize,
            status = "uploaded",
            processingTime = "1-3 business days",
            uploadedAt = DateTime.UtcNow
        };

        return (T)(object)uploadResponse;
    }
}