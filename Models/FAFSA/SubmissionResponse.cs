using System.ComponentModel.DataAnnotations;

namespace finaid.Models.FAFSA;

/// <summary>
/// Response from federal FAFSA submission API
/// </summary>
public class SubmissionResponse
{
    /// <summary>
    /// Whether the submission was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Federal confirmation number if successful
    /// </summary>
    public string? ConfirmationNumber { get; set; }
    
    /// <summary>
    /// Primary error message if submission failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// List of validation errors from federal system
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();
    
    /// <summary>
    /// List of warnings that don't prevent submission
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// When the submission was processed
    /// </summary>
    public DateTime SubmittedAt { get; set; }
    
    /// <summary>
    /// Current status of the submission
    /// </summary>
    public SubmissionStatus Status { get; set; }
    
    /// <summary>
    /// Estimated processing time
    /// </summary>
    public TimeSpan? EstimatedProcessingTime { get; set; }
    
    /// <summary>
    /// Transaction ID from federal system for tracking
    /// </summary>
    public string? TransactionId { get; set; }
    
    /// <summary>
    /// Expected Family Contribution calculated by federal system
    /// </summary>
    public decimal? CalculatedEFC { get; set; }
    
    /// <summary>
    /// Student Aid Index calculated by federal system (2024-25 and later)
    /// </summary>
    public decimal? CalculatedSAI { get; set; }
    
    /// <summary>
    /// Pell Grant eligibility determined by federal system
    /// </summary>
    public decimal? PellGrantEligibility { get; set; }
    
    /// <summary>
    /// Additional processing requirements
    /// </summary>
    public List<ProcessingRequirement> ProcessingRequirements { get; set; } = new();
    
    /// <summary>
    /// Documents required for verification
    /// </summary>
    public List<RequiredDocument> RequiredDocuments { get; set; } = new();
    
    /// <summary>
    /// Next steps for the applicant
    /// </summary>
    public List<string> NextSteps { get; set; } = new();
    
    /// <summary>
    /// Raw response data from federal API for debugging
    /// </summary>
    public string? RawResponse { get; set; }
}

/// <summary>
/// Processing requirement from federal system
/// </summary>
public class ProcessingRequirement
{
    [Required]
    public string RequirementType { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public DateTime? DueDate { get; set; }
    
    public bool IsRequired { get; set; } = true;
    
    public string? Instructions { get; set; }
    
    public string? ContactInformation { get; set; }
}

/// <summary>
/// Document required for verification
/// </summary>
public class RequiredDocument
{
    [Required]
    public string DocumentType { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public DateTime? DueDate { get; set; }
    
    public bool IsRequired { get; set; } = true;
    
    public List<string> AcceptedFormats { get; set; } = new();
    
    public string? Instructions { get; set; }
    
    public string? WhereToSubmit { get; set; }
}

/// <summary>
/// Status of a FAFSA submission
/// </summary>
public enum SubmissionStatus
{
    /// <summary>
    /// Application is in draft state, not yet submitted
    /// </summary>
    Draft,
    
    /// <summary>
    /// Validation is being performed
    /// </summary>
    ValidationPending,
    
    /// <summary>
    /// Validation failed, corrections needed
    /// </summary>
    ValidationFailed,
    
    /// <summary>
    /// Application is ready to submit
    /// </summary>
    ReadyToSubmit,
    
    /// <summary>
    /// Application has been submitted to federal system
    /// </summary>
    Submitted,
    
    /// <summary>
    /// Federal system is processing the application
    /// </summary>
    Processing,
    
    /// <summary>
    /// Processing completed successfully
    /// </summary>
    Processed,
    
    /// <summary>
    /// Application was rejected by federal system
    /// </summary>
    Rejected,
    
    /// <summary>
    /// Application processing is complete, results available
    /// </summary>
    Completed,
    
    /// <summary>
    /// Application requires verification documents
    /// </summary>
    VerificationRequired,
    
    /// <summary>
    /// Application has been corrected and resubmitted
    /// </summary>
    Corrected,
    
    /// <summary>
    /// Application is on hold pending resolution
    /// </summary>
    OnHold,
    
    /// <summary>
    /// Application was cancelled by the applicant
    /// </summary>
    Cancelled
}