using finaid.Models.FAFSA;
using FluentValidation.Results;

namespace finaid.Services.FAFSA;

/// <summary>
/// Service for submitting FAFSA applications to federal systems
/// </summary>
public interface IFAFSASubmissionService
{
    /// <summary>
    /// Submit a FAFSA application to the federal system
    /// </summary>
    /// <param name="applicationId">ID of the application to submit</param>
    /// <param name="fsaIdUsername">FSA ID username for authentication</param>
    /// <param name="digitalSignatures">Digital signatures from applicant and parents/spouse</param>
    /// <returns>Submission response with confirmation number or errors</returns>
    Task<SubmissionResponse> SubmitApplicationAsync(
        Guid applicationId, 
        string fsaIdUsername, 
        List<DigitalSignature> digitalSignatures);
    
    /// <summary>
    /// Get the current status of a submitted FAFSA application
    /// </summary>
    /// <param name="confirmationNumber">Federal confirmation number</param>
    /// <returns>Current submission status and processing details</returns>
    Task<FAFSASubmissionStatus> GetSubmissionStatusAsync(string confirmationNumber);
    
    /// <summary>
    /// Validate a FAFSA application before submission
    /// </summary>
    /// <param name="applicationId">ID of the application to validate</param>
    /// <returns>Validation results with any errors or warnings</returns>
    Task<ValidationResult> ValidateApplicationAsync(Guid applicationId);
    
    /// <summary>
    /// Check if a FAFSA application is ready for submission
    /// </summary>
    /// <param name="applicationId">ID of the application to check</param>
    /// <returns>True if the application can be submitted</returns>
    Task<bool> CanSubmitApplicationAsync(Guid applicationId);
    
    /// <summary>
    /// Submit a correction to a previously submitted FAFSA
    /// </summary>
    /// <param name="applicationId">ID of the corrected application</param>
    /// <param name="previousConfirmationNumber">Confirmation number of original submission</param>
    /// <param name="fsaIdUsername">FSA ID username for authentication</param>
    /// <param name="digitalSignatures">Updated digital signatures</param>
    /// <param name="changeSummary">Summary of changes made</param>
    /// <returns>Submission response for the correction</returns>
    Task<SubmissionResponse> SubmitCorrectionAsync(
        Guid applicationId,
        string previousConfirmationNumber,
        string fsaIdUsername,
        List<DigitalSignature> digitalSignatures,
        List<string> changeSummary);
    
    /// <summary>
    /// Retry a failed submission with the same data
    /// </summary>
    /// <param name="applicationId">ID of the application to retry</param>
    /// <param name="previousTransactionId">Transaction ID of the failed submission</param>
    /// <returns>New submission response</returns>
    Task<SubmissionResponse> RetrySubmissionAsync(Guid applicationId, string previousTransactionId);
    
    /// <summary>
    /// Cancel a submitted FAFSA application (if allowed)
    /// </summary>
    /// <param name="confirmationNumber">Federal confirmation number</param>
    /// <param name="reason">Reason for cancellation</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelSubmissionAsync(string confirmationNumber, string reason);
    
    /// <summary>
    /// Get a detailed submission history for an application
    /// </summary>
    /// <param name="applicationId">ID of the application</param>
    /// <returns>List of all submission attempts and their results</returns>
    Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid applicationId);
    
    /// <summary>
    /// Download the Student Aid Report (SAR) if available
    /// </summary>
    /// <param name="confirmationNumber">Federal confirmation number</param>
    /// <returns>SAR document data</returns>
    Task<byte[]?> DownloadStudentAidReportAsync(string confirmationNumber);
    
    /// <summary>
    /// Get all available schools for the award year
    /// </summary>
    /// <param name="awardYear">Award year</param>
    /// <param name="stateCode">Optional state filter</param>
    /// <returns>List of participating schools</returns>
    Task<List<ParticipatingSchool>> GetParticipatingSchoolsAsync(int awardYear, string? stateCode = null);
    
    /// <summary>
    /// Validate FSA ID credentials
    /// </summary>
    /// <param name="fsaIdUsername">FSA ID username</param>
    /// <param name="encryptedPin">Encrypted PIN/password</param>
    /// <returns>True if credentials are valid</returns>
    Task<bool> ValidateFSAIdAsync(string fsaIdUsername, string encryptedPin);
}

/// <summary>
/// Historical record of submission attempts
/// </summary>
public class SubmissionHistory
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public SubmissionStatus Status { get; set; }
    public string? ConfirmationNumber { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public bool IsCorrection { get; set; } = false;
    public string? PreviousConfirmationNumber { get; set; }
    public string SubmitterIPAddress { get; set; } = string.Empty;
    public string FSAIdUsername { get; set; } = string.Empty;
}

/// <summary>
/// Information about a participating school
/// </summary>
public class ParticipatingSchool
{
    public string FederalSchoolCode { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string SchoolType { get; set; } = string.Empty; // Public, Private non-profit, etc.
    public bool IsActive { get; set; } = true;
    public string? Website { get; set; }
    public string? FinancialAidOfficePhone { get; set; }
    public string? FinancialAidOfficeEmail { get; set; }
}