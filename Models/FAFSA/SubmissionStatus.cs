using System.ComponentModel.DataAnnotations;

namespace finaid.Models.FAFSA;

/// <summary>
/// Current status and details of a FAFSA submission
/// </summary>
public class FAFSASubmissionStatus
{
    [Required]
    public string ConfirmationNumber { get; set; } = string.Empty;
    
    [Required]
    public Guid ApplicationId { get; set; }
    
    [Required]
    public SubmissionStatus Status { get; set; }
    
    [Required]
    public DateTime LastUpdated { get; set; }
    
    /// <summary>
    /// When the application was initially submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }
    
    /// <summary>
    /// When processing was completed (if applicable)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Current step in the processing workflow
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;
    
    /// <summary>
    /// Percentage of processing completed (0-100)
    /// </summary>
    public int ProcessingProgress { get; set; } = 0;
    
    /// <summary>
    /// Estimated time remaining for processing
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    
    /// <summary>
    /// Status messages and updates
    /// </summary>
    public List<StatusMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// Issues or errors that need attention
    /// </summary>
    public List<ProcessingIssue> Issues { get; set; } = new();
    
    /// <summary>
    /// Documents required for completion
    /// </summary>
    public List<RequiredDocument> RequiredDocuments { get; set; } = new();
    
    /// <summary>
    /// Actions the applicant can take
    /// </summary>
    public List<AvailableAction> AvailableActions { get; set; } = new();
    
    /// <summary>
    /// Calculated financial aid results (when available)
    /// </summary>
    public FinancialAidResults? Results { get; set; }
    
    /// <summary>
    /// Contact information for assistance
    /// </summary>
    public ContactInformation? SupportContact { get; set; }
    
    /// <summary>
    /// Important deadlines related to this submission
    /// </summary>
    public List<ImportantDeadline> Deadlines { get; set; } = new();
}

/// <summary>
/// Status message with timestamp
/// </summary>
public class StatusMessage
{
    [Required]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    public MessageType Type { get; set; } = MessageType.Information;
    
    public string? Details { get; set; }
    
    public bool IsRead { get; set; } = false;
}

/// <summary>
/// Type of status message
/// </summary>
public enum MessageType
{
    Information,
    Success,
    Warning,
    Error,
    Action
}

/// <summary>
/// Processing issue that needs attention
/// </summary>
public class ProcessingIssue
{
    [Required]
    public string IssueType { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public IssueSeverity Severity { get; set; }
    
    public string? Resolution { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public bool IsResolved { get; set; } = false;
    
    public DateTime? ResolvedAt { get; set; }
    
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// Severity level of processing issues
/// </summary>
public enum IssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Action available to the applicant
/// </summary>
public class AvailableAction
{
    [Required]
    public string ActionType { get; set; } = string.Empty;
    
    [Required]
    public string Label { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? Url { get; set; }
    
    public bool RequiresAuthentication { get; set; } = true;
    
    public DateTime? AvailableUntil { get; set; }
    
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Financial aid calculation results
/// </summary>
public class FinancialAidResults
{
    /// <summary>
    /// Expected Family Contribution
    /// </summary>
    public decimal? ExpectedFamilyContribution { get; set; }
    
    /// <summary>
    /// Student Aid Index (2024-25 and later)
    /// </summary>
    public decimal? StudentAidIndex { get; set; }
    
    /// <summary>
    /// Federal Pell Grant amount
    /// </summary>
    public decimal? PellGrantAmount { get; set; }
    
    /// <summary>
    /// Federal loan eligibility amounts by type
    /// </summary>
    public Dictionary<string, decimal> LoanEligibility { get; set; } = new();
    
    /// <summary>
    /// Work-Study eligibility
    /// </summary>
    public decimal? WorkStudyEligibility { get; set; }
    
    /// <summary>
    /// Other federal aid eligibility
    /// </summary>
    public Dictionary<string, decimal> OtherAidEligibility { get; set; } = new();
    
    /// <summary>
    /// When these results were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; }
    
    /// <summary>
    /// Whether results are final or preliminary
    /// </summary>
    public bool IsFinal { get; set; } = false;
}

/// <summary>
/// Contact information for support
/// </summary>
public class ContactInformation
{
    public string? PhoneNumber { get; set; }
    
    public string? Email { get; set; }
    
    public string? Website { get; set; }
    
    public string? Address { get; set; }
    
    public string? Hours { get; set; }
    
    public List<string> PreferredContactMethods { get; set; } = new();
}

/// <summary>
/// Important deadline information
/// </summary>
public class ImportantDeadline
{
    [Required]
    public string DeadlineType { get; set; } = string.Empty;
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? Consequences { get; set; }
    
    public bool IsCritical { get; set; } = false;
    
    public int DaysRemaining => (Date - DateTime.Now).Days;
}