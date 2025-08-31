using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using finaid.Configuration;
using finaid.Models.Federal;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace finaid.Services.Federal;

public class FederalApiClientService : IFederalApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly FederalApiConfiguration _configuration;
    private readonly ILogger<FederalApiClientService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private AuthenticationResponse? _currentToken;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);

    public FederalApiClientService(
        HttpClient httpClient,
        IOptions<FederalApiConfiguration> configuration,
        ILogger<FederalApiClientService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        ConfigureHttpClient();
    }

    public bool IsAuthenticated => _currentToken?.IsExpired == false;

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogDebug("Starting GET request to {Endpoint} [RequestId: {RequestId}]", endpoint, requestId);

            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            var result = await ProcessResponseAsync<T>(response, requestId, stopwatch.Elapsed);

            _logger.LogDebug("GET request completed for {Endpoint} [RequestId: {RequestId}] - Success: {IsSuccess}",
                endpoint, requestId, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request failed for {Endpoint} [RequestId: {RequestId}]", endpoint, requestId);
            return CreateErrorResponse<T>(ex, requestId, stopwatch.Elapsed);
        }
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? payload = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogDebug("Starting POST request to {Endpoint} [RequestId: {RequestId}]", endpoint, requestId);

            await EnsureAuthenticatedAsync(cancellationToken);

            var content = payload != null 
                ? new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
                : null;

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var result = await ProcessResponseAsync<T>(response, requestId, stopwatch.Elapsed);

            _logger.LogDebug("POST request completed for {Endpoint} [RequestId: {RequestId}] - Success: {IsSuccess}",
                endpoint, requestId, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed for {Endpoint} [RequestId: {RequestId}]", endpoint, requestId);
            return CreateErrorResponse<T>(ex, requestId, stopwatch.Elapsed);
        }
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? payload = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogDebug("Starting PUT request to {Endpoint} [RequestId: {RequestId}]", endpoint, requestId);

            await EnsureAuthenticatedAsync(cancellationToken);

            var content = payload != null 
                ? new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
                : null;

            var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
            var result = await ProcessResponseAsync<T>(response, requestId, stopwatch.Elapsed);

            _logger.LogDebug("PUT request completed for {Endpoint} [RequestId: {RequestId}] - Success: {IsSuccess}",
                endpoint, requestId, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT request failed for {Endpoint} [RequestId: {RequestId}]", endpoint, requestId);
            return CreateErrorResponse<T>(ex, requestId, stopwatch.Elapsed);
        }
    }

    public async Task<ApiResponse<T>> UploadFileAsync<T>(
        string endpoint,
        Stream fileContent,
        string fileName,
        string contentType,
        Dictionary<string, string>? additionalFields = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogDebug("Starting file upload to {Endpoint} [RequestId: {RequestId}] - File: {FileName}",
                endpoint, requestId, fileName);

            await EnsureAuthenticatedAsync(cancellationToken);

            using var formContent = new MultipartFormDataContent();
            
            var fileStreamContent = new StreamContent(fileContent);
            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            formContent.Add(fileStreamContent, "file", fileName);

            if (additionalFields != null)
            {
                foreach (var field in additionalFields)
                {
                    formContent.Add(new StringContent(field.Value), field.Key);
                }
            }

            var response = await _httpClient.PostAsync(endpoint, formContent, cancellationToken);
            var result = await ProcessResponseAsync<T>(response, requestId, stopwatch.Elapsed);

            _logger.LogDebug("File upload completed for {Endpoint} [RequestId: {RequestId}] - Success: {IsSuccess}",
                endpoint, requestId, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed for {Endpoint} [RequestId: {RequestId}]", endpoint, requestId);
            return CreateErrorResponse<T>(ex, requestId, stopwatch.Elapsed);
        }
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating connection to federal API");
            
            // Try to authenticate first
            var authResult = await AuthenticateAsync(cancellationToken);
            if (!authResult)
            {
                _logger.LogWarning("Connection validation failed - authentication unsuccessful");
                return false;
            }

            // Try a simple health check endpoint
            var response = await _httpClient.GetAsync("health", cancellationToken);
            var isValid = response.IsSuccessStatusCode;

            _logger.LogInformation("Connection validation {Result}", isValid ? "successful" : "failed");
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection validation failed with exception");
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Refreshing authentication token");
            _currentToken = null;
            return await AuthenticateAsync(cancellationToken);
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.RequestTimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FinAid-Assistant", "1.0"));
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (IsAuthenticated) return;

        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the lock
            if (IsAuthenticated) return;

            var authenticated = await AuthenticateAsync(cancellationToken);
            if (!authenticated)
            {
                throw new UnauthorizedAccessException("Failed to authenticate with federal API");
            }
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Authenticating with federal API using client credentials");

            var authRequest = new AuthenticationRequest
            {
                ClientId = _configuration.ClientId,
                ClientSecret = _configuration.ClientSecret,
                GrantType = "client_credentials",
                Scope = _configuration.Scope
            };

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", authRequest.GrantType),
                new KeyValuePair<string, string>("client_id", authRequest.ClientId),
                new KeyValuePair<string, string>("client_secret", authRequest.ClientSecret),
                new KeyValuePair<string, string>("scope", authRequest.Scope)
            });

            using var tokenClient = new HttpClient();
            var response = await tokenClient.PostAsync(_configuration.TokenUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Authentication failed - Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent, _jsonOptions);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogError("Authentication failed - Invalid token response");
                return false;
            }

            _currentToken = tokenResponse;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

            _logger.LogInformation("Successfully authenticated with federal API - Token expires at {ExpiresAt}",
                tokenResponse.ExpiresAt);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed with exception");
            return false;
        }
    }

    private async Task<ApiResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response, string requestId, TimeSpan duration)
    {
        var apiResponse = new ApiResponse<T>
        {
            StatusCode = response.StatusCode,
            RequestId = requestId,
            Duration = duration,
            ResponseTime = DateTime.UtcNow
        };

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                
                if (typeof(T) == typeof(string))
                {
                    apiResponse.Data = (T)(object)content;
                }
                else if (!string.IsNullOrWhiteSpace(content))
                {
                    apiResponse.Data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                }

                apiResponse.IsSuccess = true;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response [RequestId: {RequestId}]", requestId);
                apiResponse.IsSuccess = false;
                apiResponse.ErrorMessage = "Failed to parse API response";
                apiResponse.Errors.Add(ex.Message);
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("API request failed [RequestId: {RequestId}] - Status: {StatusCode}, Error: {Error}",
                requestId, response.StatusCode, errorContent);

            apiResponse.IsSuccess = false;
            apiResponse.ErrorMessage = $"API request failed with status {response.StatusCode}";
            
            if (!string.IsNullOrWhiteSpace(errorContent))
            {
                apiResponse.Errors.Add(errorContent);
            }
        }

        return apiResponse;
    }

    private static ApiResponse<T> CreateErrorResponse<T>(Exception ex, string requestId, TimeSpan duration)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorMessage = ex.Message,
            Errors = new List<string> { ex.ToString() },
            StatusCode = HttpStatusCode.InternalServerError,
            RequestId = requestId,
            Duration = duration,
            ResponseTime = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _tokenSemaphore?.Dispose();
        _httpClient?.Dispose();
    }
}