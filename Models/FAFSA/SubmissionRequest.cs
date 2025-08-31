using System.ComponentModel.DataAnnotations;
using finaid.Models.Application;

namespace finaid.Models.FAFSA;

/// <summary>
/// Request payload for FAFSA submission to federal systems
/// </summary>
public class SubmissionRequest
{
    [Required]
    public Guid ApplicationId { get; set; }
    
    [Required]
    public int AwardYear { get; set; }
    
    [Required]
    public StudentInformation StudentInfo { get; set; } = null!;
    
    [Required]
    public FinancialInformation FinancialInfo { get; set; } = null!;
    
    public List<FamilyInformation> FamilyMembers { get; set; } = new();
    
    [Required]
    public List<SchoolSelection> SchoolSelections { get; set; } = new();
    
    /// <summary>
    /// Digital signature data for the submission
    /// </summary>
    public DigitalSignature StudentSignature { get; set; } = null!;
    
    /// <summary>
    /// Parent signature for dependent students
    /// </summary>
    public DigitalSignature? ParentSignature { get; set; }
    
    /// <summary>
    /// Spouse signature for married students
    /// </summary>
    public DigitalSignature? SpouseSignature { get; set; }
    
    /// <summary>
    /// Federal Student Aid ID username for authentication
    /// </summary>
    [Required]
    public string FSAIdUsername { get; set; } = string.Empty;
    
    /// <summary>
    /// Submission timestamp
    /// </summary>
    public DateTime SubmissionTimestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// IP address of the submitter for audit purposes
    /// </summary>
    public string? SubmitterIPAddress { get; set; }
    
    /// <summary>
    /// User agent of the submitter for audit purposes
    /// </summary>
    public string? SubmitterUserAgent { get; set; }
    
    /// <summary>
    /// Whether this is a correction to a previously submitted FAFSA
    /// </summary>
    public bool IsCorrection { get; set; } = false;
    
    /// <summary>
    /// Previous confirmation number if this is a correction
    /// </summary>
    public string? PreviousConfirmationNumber { get; set; }
    
    /// <summary>
    /// Summary of changes made if this is a correction
    /// </summary>
    public List<string> ChangeSummary { get; set; } = new();
    
    /// <summary>
    /// Additional verification documents if required
    /// </summary>
    public List<DocumentReference> SupportingDocuments { get; set; } = new();
}

/// <summary>
/// Digital signature information
/// </summary>
public class DigitalSignature
{
    [Required]
    public string SignerName { get; set; } = string.Empty;
    
    [Required]
    public string SignerSSN { get; set; } = string.Empty; // Encrypted
    
    [Required]
    public DateTime SignedAt { get; set; }
    
    [Required]
    public string IPAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// FSA ID username used for signing
    /// </summary>
    [Required]
    public string FSAIdUsername { get; set; } = string.Empty;
    
    /// <summary>
    /// PIN or password verification (encrypted)
    /// </summary>
    public string? EncryptedPIN { get; set; }
    
    /// <summary>
    /// Signature method used (FSA ID, PIN, etc.)
    /// </summary>
    public string SignatureMethod { get; set; } = "FSA_ID";
    
    /// <summary>
    /// Browser/device information
    /// </summary>
    public string? DeviceInformation { get; set; }
}

/// <summary>
/// Reference to a supporting document
/// </summary>
public class DocumentReference
{
    [Required]
    public string DocumentId { get; set; } = string.Empty;
    
    [Required]
    public string DocumentType { get; set; } = string.Empty;
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTime UploadedAt { get; set; }
    
    public long FileSizeBytes { get; set; }
    
    public string? ContentHash { get; set; }
}