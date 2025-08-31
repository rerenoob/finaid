using finaid.Data;
using finaid.Extensions;
using finaid.Models.FAFSA;
using finaid.Services.Federal;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace finaid.Services.FAFSA;

/// <summary>
/// Service for submitting FAFSA applications to federal systems
/// </summary>
public class FAFSASubmissionService : IFAFSASubmissionService
{
    private readonly ApplicationDbContext _context;
    private readonly IFederalApiClient _federalApiClient;
    private readonly IMemoryCache _cache;
    private readonly FAFSAValidationService _validationService;
    private readonly ILogger<FAFSASubmissionService> _logger;

    public FAFSASubmissionService(
        ApplicationDbContext context,
        IFederalApiClient federalApiClient,
        IMemoryCache cache,
        FAFSAValidationService validationService,
        ILogger<FAFSASubmissionService> logger)
    {
        _context = context;
        _federalApiClient = federalApiClient;
        _cache = cache;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<SubmissionResponse> SubmitApplicationAsync(
        Guid applicationId, 
        string fsaIdUsername, 
        List<DigitalSignature> digitalSignatures)
    {
        _logger.LogInformation("Starting FAFSA submission for application {ApplicationId}", applicationId);

        try
        {
            // Validate that the application exists and is complete
            var canSubmit = await CanSubmitApplicationAsync(applicationId);
            if (!canSubmit)
            {
                return new SubmissionResponse
                {
                    Success = false,
                    ErrorMessage = "Application is not ready for submission",
                    Status = SubmissionStatus.ValidationFailed
                };
            }

            // Load the application data
            var application = await LoadApplicationDataAsync(applicationId);
            if (application == null)
            {
                return new SubmissionResponse
                {
                    Success = false,
                    ErrorMessage = "Application not found",
                    Status = SubmissionStatus.ValidationFailed
                };
            }

            // Perform final validation
            var validationResult = await ValidateApplicationAsync(applicationId);
            if (!validationResult.IsValid)
            {
                return new SubmissionResponse
                {
                    Success = false,
                    ErrorMessage = "Application failed validation",
                    ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList(),
                    Status = SubmissionStatus.ValidationFailed
                };
            }

            // Build submission request
            var submissionRequest = await BuildSubmissionRequestAsync(
                application, fsaIdUsername, digitalSignatures);

            // Submit to federal API
            var response = await SubmitToFederalSystemAsync(submissionRequest);

            // Update application status
            await UpdateApplicationStatusAsync(applicationId, response);

            // Log submission attempt
            await LogSubmissionAttemptAsync(applicationId, submissionRequest, response);

            _logger.LogInformation("FAFSA submission completed for application {ApplicationId}. " +
                                 "Success: {Success}, Confirmation: {ConfirmationNumber}",
                applicationId, response.Success, response.ConfirmationNumber);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting FAFSA application {ApplicationId}", applicationId);
            
            return new SubmissionResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while submitting the application",
                Status = SubmissionStatus.ValidationFailed
            };
        }
    }

    public async Task<FAFSASubmissionStatus> GetSubmissionStatusAsync(string confirmationNumber)
    {
        _logger.LogDebug("Getting submission status for confirmation number {ConfirmationNumber}", 
            confirmationNumber);

        try
        {
            // Check cache first
            var cacheKey = $"fafsa_status_{confirmationNumber}";
            if (_cache.TryGetValue(cacheKey, out FAFSASubmissionStatus? cachedStatus))
            {
                return cachedStatus!;
            }

            // Call federal API to get current status
            var apiResponse = await _federalApiClient.GetAsync<FAFSASubmissionStatus>(
                $"/fafsa/status/{confirmationNumber}");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                // Cache the status for 5 minutes
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));
                
                _cache.Set(cacheKey, apiResponse.Data, cacheOptions);
                
                return apiResponse.Data;
            }

            // If API call fails, try to get status from local database
            return await GetLocalSubmissionStatusAsync(confirmationNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission status for {ConfirmationNumber}", 
                confirmationNumber);
            
            // Return a default error status
            return new FAFSASubmissionStatus
            {
                ConfirmationNumber = confirmationNumber,
                Status = SubmissionStatus.Processing,
                LastUpdated = DateTime.UtcNow,
                Messages = new List<StatusMessage>
                {
                    new()
                    {
                        Message = "Unable to retrieve current status. Please try again later.",
                        Type = MessageType.Warning,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };
        }
    }

    public async Task<ValidationResult> ValidateApplicationAsync(Guid applicationId)
    {
        return await _validationService.ValidateForSubmissionAsync(applicationId);
    }

    public async Task<bool> CanSubmitApplicationAsync(Guid applicationId)
    {
        try
        {
            var validation = await ValidateApplicationAsync(applicationId);
            return validation.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if application {ApplicationId} can be submitted", 
                applicationId);
            return false;
        }
    }

    public async Task<SubmissionResponse> SubmitCorrectionAsync(
        Guid applicationId,
        string previousConfirmationNumber,
        string fsaIdUsername,
        List<DigitalSignature> digitalSignatures,
        List<string> changeSummary)
    {
        _logger.LogInformation("Starting FAFSA correction for application {ApplicationId}, " +
                             "previous confirmation: {PreviousConfirmationNumber}",
            applicationId, previousConfirmationNumber);

        try
        {
            // Load application data
            var application = await LoadApplicationDataAsync(applicationId);
            if (application == null)
            {
                return new SubmissionResponse
                {
                    Success = false,
                    ErrorMessage = "Application not found",
                    Status = SubmissionStatus.ValidationFailed
                };
            }

            // Build correction request
            var submissionRequest = await BuildSubmissionRequestAsync(
                application, fsaIdUsername, digitalSignatures);
            
            submissionRequest.IsCorrection = true;
            submissionRequest.PreviousConfirmationNumber = previousConfirmationNumber;
            submissionRequest.ChangeSummary = changeSummary;

            // Submit correction to federal API
            var response = await SubmitToFederalSystemAsync(submissionRequest);
            response.Status = SubmissionStatus.Corrected;

            // Update application status
            await UpdateApplicationStatusAsync(applicationId, response);

            // Log correction attempt
            await LogSubmissionAttemptAsync(applicationId, submissionRequest, response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting FAFSA correction for application {ApplicationId}", 
                applicationId);
            
            return new SubmissionResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while submitting the correction",
                Status = SubmissionStatus.ValidationFailed
            };
        }
    }

    public async Task<SubmissionResponse> RetrySubmissionAsync(Guid applicationId, string previousTransactionId)
    {
        _logger.LogInformation("Retrying FAFSA submission for application {ApplicationId}, " +
                             "previous transaction: {PreviousTransactionId}",
            applicationId, previousTransactionId);

        // Get the previous submission attempt
        var previousAttempt = await _context.Set<SubmissionHistory>()
            .Where(h => h.ApplicationId == applicationId && h.TransactionId == previousTransactionId)
            .OrderByDescending(h => h.SubmittedAt)
            .FirstOrDefaultAsync();

        if (previousAttempt == null)
        {
            return new SubmissionResponse
            {
                Success = false,
                ErrorMessage = "Previous submission attempt not found",
                Status = SubmissionStatus.ValidationFailed
            };
        }

        // Retry with the same FSA ID but new signatures would be required
        var digitalSignatures = new List<DigitalSignature>(); // Would need to get new signatures
        
        return await SubmitApplicationAsync(applicationId, previousAttempt.FSAIdUsername, digitalSignatures);
    }

    public async Task<bool> CancelSubmissionAsync(string confirmationNumber, string reason)
    {
        _logger.LogInformation("Attempting to cancel FAFSA submission {ConfirmationNumber}", 
            confirmationNumber);

        try
        {
            var cancelRequest = new
            {
                ConfirmationNumber = confirmationNumber,
                Reason = reason,
                CancelledAt = DateTime.UtcNow
            };

            var response = await _federalApiClient.PostAsync<object>(
                "/fafsa/cancel", cancelRequest);

            return response.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling FAFSA submission {ConfirmationNumber}", 
                confirmationNumber);
            return false;
        }
    }

    public async Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid applicationId)
    {
        return await _context.Set<SubmissionHistory>()
            .Where(h => h.ApplicationId == applicationId)
            .OrderByDescending(h => h.SubmittedAt)
            .ToListAsync();
    }

    public async Task<byte[]?> DownloadStudentAidReportAsync(string confirmationNumber)
    {
        try
        {
            var response = await _federalApiClient.GetAsync<byte[]>(
                $"/fafsa/sar/{confirmationNumber}");

            return response.IsSuccess ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading SAR for {ConfirmationNumber}", confirmationNumber);
            return null;
        }
    }

    public async Task<List<ParticipatingSchool>> GetParticipatingSchoolsAsync(int awardYear, string? stateCode = null)
    {
        var cacheKey = $"participating_schools_{awardYear}_{stateCode ?? "all"}";
        
        if (_cache.TryGetValue(cacheKey, out List<ParticipatingSchool>? cachedSchools))
        {
            return cachedSchools!;
        }

        try
        {
            var url = $"/schools/participating/{awardYear}";
            if (!string.IsNullOrEmpty(stateCode))
            {
                url += $"?state={stateCode}";
            }

            var response = await _federalApiClient.GetAsync<List<ParticipatingSchool>>(url);

            if (response.IsSuccess && response.Data != null)
            {
                // Cache for 24 hours
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(24));
                
                _cache.Set(cacheKey, response.Data, cacheOptions);
                
                return response.Data;
            }

            return new List<ParticipatingSchool>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participating schools for award year {AwardYear}", awardYear);
            return new List<ParticipatingSchool>();
        }
    }

    public async Task<bool> ValidateFSAIdAsync(string fsaIdUsername, string encryptedPin)
    {
        try
        {
            var validationRequest = new
            {
                Username = fsaIdUsername,
                EncryptedPin = encryptedPin
            };

            var response = await _federalApiClient.PostAsync<bool>(
                "/auth/validate-fsa-id", validationRequest);

            return response.IsSuccess && response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating FSA ID for username {FSAIdUsername}", fsaIdUsername);
            return false;
        }
    }

    // Private helper methods

    private async Task<Models.Application.FAFSAApplication?> LoadApplicationDataAsync(Guid applicationId)
    {
        return await _context.Set<Models.Application.FAFSAApplication>()
            .FirstOrDefaultAsync(a => a.Id == applicationId);
    }

    private Task<SubmissionRequest> BuildSubmissionRequestAsync(
        Models.Application.FAFSAApplication application,
        string fsaIdUsername,
        List<DigitalSignature> digitalSignatures)
    {
        // Parse form data to get structured FAFSA data
        var formData = string.IsNullOrEmpty(application.FormDataJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(application.FormDataJson) 
                ?? new Dictionary<string, object>();

        // This would need to be implemented to extract structured data from the JSON
        // For now, return a basic request structure
        var result = new SubmissionRequest
        {
            ApplicationId = application.Id,
            AwardYear = application.AwardYear,
            FSAIdUsername = fsaIdUsername,
            StudentSignature = digitalSignatures.First(), // Would need proper logic to assign signatures
            SubmissionTimestamp = DateTime.UtcNow
            // Additional fields would be populated from the parsed form data
        };
        
        return Task.FromResult(result);
    }

    private async Task<SubmissionResponse> SubmitToFederalSystemAsync(SubmissionRequest request)
    {
        try
        {
            _logger.LogDebug("Submitting FAFSA to federal system for application {ApplicationId}", 
                request.ApplicationId);

            // Convert request to API format
            var apiRequest = request.ToApiSubmissionFormat();

            // Submit to federal API
            var apiResponse = await _federalApiClient.PostAsync<SubmissionResponse>(
                "/fafsa/submit", apiRequest);

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return apiResponse.Data;
            }

            // Handle API errors
            return new SubmissionResponse
            {
                Success = false,
                ErrorMessage = apiResponse.ErrorMessage ?? "Submission failed",
                Status = SubmissionStatus.ValidationFailed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling federal submission API");
            
            return new SubmissionResponse
            {
                Success = false,
                ErrorMessage = "Federal system temporarily unavailable",
                Status = SubmissionStatus.ValidationFailed
            };
        }
    }

    private async Task UpdateApplicationStatusAsync(Guid applicationId, SubmissionResponse response)
    {
        var application = await _context.Set<Models.Application.FAFSAApplication>()
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application != null)
        {
            if (response.Success)
            {
                application.Status = Models.Application.ApplicationStatus.Submitted;
                application.SubmittedAt = response.SubmittedAt;
                application.ConfirmationNumber = response.ConfirmationNumber;
                application.ProcessingNotes = "Successfully submitted to federal system";
            }
            else
            {
                application.Status = Models.Application.ApplicationStatus.Failed;
                application.RejectionReason = response.ErrorMessage;
            }

            await _context.SaveChangesAsync();
        }
    }

    private async Task LogSubmissionAttemptAsync(
        Guid applicationId, 
        SubmissionRequest request, 
        SubmissionResponse response)
    {
        var history = new SubmissionHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            SubmittedAt = request.SubmissionTimestamp,
            Status = response.Status,
            ConfirmationNumber = response.ConfirmationNumber,
            TransactionId = response.TransactionId,
            ErrorMessage = response.ErrorMessage,
            ValidationErrors = response.ValidationErrors,
            IsCorrection = request.IsCorrection,
            PreviousConfirmationNumber = request.PreviousConfirmationNumber,
            SubmitterIPAddress = request.SubmitterIPAddress ?? "",
            FSAIdUsername = request.FSAIdUsername
        };

        _context.Set<SubmissionHistory>().Add(history);
        await _context.SaveChangesAsync();
    }

    private async Task<FAFSASubmissionStatus> GetLocalSubmissionStatusAsync(string confirmationNumber)
    {
        // Try to get status from local database as fallback
        var history = await _context.Set<SubmissionHistory>()
            .Where(h => h.ConfirmationNumber == confirmationNumber)
            .OrderByDescending(h => h.SubmittedAt)
            .FirstOrDefaultAsync();

        if (history != null)
        {
            return new FAFSASubmissionStatus
            {
                ConfirmationNumber = confirmationNumber,
                ApplicationId = history.ApplicationId,
                Status = history.Status,
                SubmittedAt = history.SubmittedAt,
                LastUpdated = history.SubmittedAt,
                Messages = new List<StatusMessage>
                {
                    new()
                    {
                        Message = "Application submitted successfully",
                        Type = MessageType.Success,
                        Timestamp = history.SubmittedAt
                    }
                }
            };
        }

        // Return default status if not found
        return new FAFSASubmissionStatus
        {
            ConfirmationNumber = confirmationNumber,
            Status = SubmissionStatus.Processing,
            LastUpdated = DateTime.UtcNow,
            Messages = new List<StatusMessage>
            {
                new()
                {
                    Message = "Status information not available",
                    Type = MessageType.Warning,
                    Timestamp = DateTime.UtcNow
                }
            }
        };
    }
}