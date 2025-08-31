using System.Text.Json.Serialization;

namespace finaid.Models.Federal;

/// <summary>
/// Base class for all Federal API response models
/// </summary>
public abstract class FederalApiResponse
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Base class for paginated Federal API responses
/// </summary>
public abstract class PaginatedFederalApiResponse : FederalApiResponse
{
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
    
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
    
    [JsonPropertyName("hasNext")]
    public bool HasNext { get; set; }
    
    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious { get; set; }
}

#region Eligibility Models

/// <summary>
/// Student eligibility check request
/// </summary>
public class EligibilityRequest
{
    [JsonPropertyName("ssn")]
    public string SSN { get; set; } = string.Empty;
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("dateOfBirth")]
    public DateTime DateOfBirth { get; set; }
    
    [JsonPropertyName("awardYear")]
    public string AwardYear { get; set; } = string.Empty;
}

/// <summary>
/// Student eligibility check response
/// </summary>
public class EligibilityResponse : FederalApiResponse
{
    [JsonPropertyName("studentId")]
    public string StudentId { get; set; } = string.Empty;
    
    [JsonPropertyName("eligibilityStatus")]
    public EligibilityStatus EligibilityStatus { get; set; }
    
    [JsonPropertyName("citizenshipStatus")]
    public CitizenshipStatus CitizenshipStatus { get; set; }
    
    [JsonPropertyName("dependencyStatus")]
    public DependencyStatus DependencyStatus { get; set; }
    
    [JsonPropertyName("priorYearIncome")]
    public decimal? PriorYearIncome { get; set; }
    
    [JsonPropertyName("expectedFamilyContribution")]
    public decimal? ExpectedFamilyContribution { get; set; }
    
    [JsonPropertyName("pellGrant")]
    public PellGrantInfo? PellGrant { get; set; }
    
    [JsonPropertyName("flags")]
    public List<EligibilityFlag> Flags { get; set; } = new();
}

public class PellGrantInfo
{
    [JsonPropertyName("eligible")]
    public bool Eligible { get; set; }
    
    [JsonPropertyName("maxAward")]
    public decimal MaxAward { get; set; }
    
    [JsonPropertyName("estimatedAward")]
    public decimal EstimatedAward { get; set; }
    
    [JsonPropertyName("lifetimeEligibilityUsed")]
    public decimal LifetimeEligibilityUsed { get; set; }
}

public enum EligibilityStatus
{
    Eligible,
    Ineligible,
    Pending,
    RequiresVerification
}

public enum CitizenshipStatus
{
    Citizen,
    EligibleNonCitizen,
    Ineligible
}

public enum DependencyStatus
{
    Dependent,
    Independent,
    Unknown
}

public class EligibilityFlag
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty; // Warning, Error, Info
}

#endregion

#region Application Models

/// <summary>
/// FAFSA application submission request
/// </summary>
public class ApplicationSubmissionRequest
{
    [JsonPropertyName("applicationYear")]
    public string ApplicationYear { get; set; } = string.Empty;
    
    [JsonPropertyName("studentInfo")]
    public StudentInfo StudentInfo { get; set; } = new();
    
    [JsonPropertyName("financialInfo")]
    public FinancialInfo FinancialInfo { get; set; } = new();
    
    [JsonPropertyName("schoolCodes")]
    public List<string> SchoolCodes { get; set; } = new();
    
    [JsonPropertyName("parentInfo")]
    public ParentInfo? ParentInfo { get; set; }
    
    [JsonPropertyName("signature")]
    public DigitalSignature Signature { get; set; } = new();
}

public class StudentInfo
{
    [JsonPropertyName("ssn")]
    public string SSN { get; set; } = string.Empty;
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("dateOfBirth")]
    public DateTime DateOfBirth { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("address")]
    public AddressInfo Address { get; set; } = new();
    
    [JsonPropertyName("maritalStatus")]
    public MaritalStatus MaritalStatus { get; set; }
}

public class FinancialInfo
{
    [JsonPropertyName("studentIncome")]
    public decimal StudentIncome { get; set; }
    
    [JsonPropertyName("studentAssets")]
    public decimal StudentAssets { get; set; }
    
    [JsonPropertyName("parentIncome")]
    public decimal? ParentIncome { get; set; }
    
    [JsonPropertyName("parentAssets")]
    public decimal? ParentAssets { get; set; }
    
    [JsonPropertyName("taxYear")]
    public int TaxYear { get; set; }
    
    [JsonPropertyName("filedTaxReturn")]
    public bool FiledTaxReturn { get; set; }
}

public class ParentInfo
{
    [JsonPropertyName("parent1")]
    public ParentDetails Parent1 { get; set; } = new();
    
    [JsonPropertyName("parent2")]
    public ParentDetails? Parent2 { get; set; }
    
    [JsonPropertyName("maritalStatus")]
    public MaritalStatus MaritalStatus { get; set; }
}

public class ParentDetails
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("ssn")]
    public string SSN { get; set; } = string.Empty;
    
    [JsonPropertyName("dateOfBirth")]
    public DateTime DateOfBirth { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class AddressInfo
{
    [JsonPropertyName("street1")]
    public string Street1 { get; set; } = string.Empty;
    
    [JsonPropertyName("street2")]
    public string? Street2 { get; set; }
    
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;
    
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
    
    [JsonPropertyName("zipCode")]
    public string ZipCode { get; set; } = string.Empty;
    
    [JsonPropertyName("country")]
    public string Country { get; set; } = "US";
}

public class DigitalSignature
{
    [JsonPropertyName("signedAt")]
    public DateTime SignedAt { get; set; }
    
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; } = string.Empty;
    
    [JsonPropertyName("consentGiven")]
    public bool ConsentGiven { get; set; }
}

public enum MaritalStatus
{
    Single,
    Married,
    Divorced,
    Widowed,
    Separated
}

/// <summary>
/// FAFSA application submission response
/// </summary>
public class ApplicationSubmissionResponse : FederalApiResponse
{
    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;
    
    [JsonPropertyName("confirmationNumber")]
    public string ConfirmationNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("submissionStatus")]
    public SubmissionStatus SubmissionStatus { get; set; }
    
    [JsonPropertyName("estimatedProcessingTime")]
    public string EstimatedProcessingTime { get; set; } = string.Empty;
    
    [JsonPropertyName("nextSteps")]
    public List<string> NextSteps { get; set; } = new();
}

public enum SubmissionStatus
{
    Submitted,
    Rejected,
    RequiresCorrection,
    Processing
}

#endregion

#region Status Models

/// <summary>
/// Application status check response
/// </summary>
public class ApplicationStatusResponse : FederalApiResponse
{
    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;
    
    [JsonPropertyName("confirmationNumber")]
    public string ConfirmationNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("currentStatus")]
    public ApplicationCurrentStatus CurrentStatus { get; set; }
    
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
    
    [JsonPropertyName("processingSteps")]
    public List<ProcessingStep> ProcessingSteps { get; set; } = new();
    
    [JsonPropertyName("sapIssues")]
    public List<SapIssue> SapIssues { get; set; } = new();
    
    [JsonPropertyName("verificationRequired")]
    public bool VerificationRequired { get; set; }
    
    [JsonPropertyName("isirAvailable")]
    public bool IsirAvailable { get; set; }
    
    [JsonPropertyName("expectedCompletionDate")]
    public DateTime? ExpectedCompletionDate { get; set; }
}

public enum ApplicationCurrentStatus
{
    Received,
    Processing,
    Complete,
    Rejected,
    RequiresCorrection,
    UnderReview
}

public class ProcessingStep
{
    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class SapIssue
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;
    
    [JsonPropertyName("resolutionRequired")]
    public bool ResolutionRequired { get; set; }
}

#endregion

#region Document Models

/// <summary>
/// Document upload request metadata
/// </summary>
public class DocumentUploadRequest
{
    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;
    
    [JsonPropertyName("documentType")]
    public FederalDocumentType DocumentType { get; set; }
    
    [JsonPropertyName("taxYear")]
    public int? TaxYear { get; set; }
    
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;
    
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }
    
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;
    
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }
}

public enum FederalDocumentType
{
    TaxReturn,
    W2Form,
    BankStatement,
    VerificationWorksheet,
    IdentityDocument,
    Other
}

/// <summary>
/// Document upload response
/// </summary>
public class DocumentUploadResponse : FederalApiResponse
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;
    
    [JsonPropertyName("uploadUrl")]
    public string? UploadUrl { get; set; }
    
    [JsonPropertyName("uploadStatus")]
    public DocumentUploadStatus UploadStatus { get; set; }
    
    [JsonPropertyName("processingTime")]
    public string ProcessingTime { get; set; } = string.Empty;
}

public enum DocumentUploadStatus
{
    Uploaded,
    Processing,
    Verified,
    Rejected,
    RequiresReview
}

#endregion

#region Error Models

/// <summary>
/// Federal API error response
/// </summary>
public class FederalApiErrorResponse : FederalApiResponse
{
    [JsonPropertyName("error")]
    public ApiError Error { get; set; } = new();
}

public class ApiError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("details")]
    public string? Details { get; set; }
    
    [JsonPropertyName("field")]
    public string? Field { get; set; }
    
    [JsonPropertyName("validationErrors")]
    public List<ValidationError>? ValidationErrors { get; set; }
}

public class ValidationError
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("rejectedValue")]
    public object? RejectedValue { get; set; }
}

#endregion

#region Rate Limiting Models

/// <summary>
/// Rate limiting information from API headers
/// </summary>
public class RateLimitInfo
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTime ResetTime { get; set; }
    public int RetryAfterSeconds { get; set; }
    public string? RequestType { get; set; }
}

#endregion