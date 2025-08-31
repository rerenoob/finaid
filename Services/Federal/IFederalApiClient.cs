using finaid.Models.Federal;

namespace finaid.Services.Federal;

public interface IFederalApiClient
{
    /// <summary>
    /// Performs a GET request to the federal API
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API response with data or error information</returns>
    Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a POST request to the federal API
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="payload">Request payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API response with data or error information</returns>
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? payload = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a PUT request to the federal API
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="payload">Request payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API response with data or error information</returns>
    Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? payload = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to the federal API
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="fileContent">File content stream</param>
    /// <param name="fileName">File name</param>
    /// <param name="contentType">File content type</param>
    /// <param name="additionalFields">Additional form fields</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API response with data or error information</returns>
    Task<ApiResponse<T>> UploadFileAsync<T>(
        string endpoint,
        Stream fileContent,
        string fileName,
        string contentType,
        Dictionary<string, string>? additionalFields = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the connection to the federal API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is valid, false otherwise</returns>
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current authentication status
    /// </summary>
    /// <returns>True if authenticated, false otherwise</returns>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Forces token refresh
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if refresh successful</returns>
    Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);
}