using finaid.Services.Documents;
using finaid.Models.Document;

namespace finaid.Models.Documents;

public class VerificationResult
{
    public Guid DocumentId { get; set; }
    public DocumentVerificationStatus Status { get; set; }
    public decimal OverallScore { get; set; }
    public List<VerificationCheck> Checks { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public DateTime VerifiedAt { get; set; }
    public bool RequiresManualReview { get; set; }
    public string? VerifierNotes { get; set; }
    public string? VerifierUserId { get; set; }
    public List<string> RequiredCorrections { get; set; } = new();
}

public enum DocumentVerificationStatus
{
    Pending,
    InProgress,
    AutoApproved,
    ManualReviewRequired,
    Approved,
    Rejected,
    Expired
}

public class VerificationCheck
{
    public string CheckName { get; set; } = null!;
    public bool Passed { get; set; }
    public decimal Confidence { get; set; }
    public string? Details { get; set; }
    public VerificationCheckType Type { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
}

public class VerificationRule
{
    public string RuleName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DocumentType ApplicableDocumentType { get; set; }
    public VerificationCheckType CheckType { get; set; }
    public bool IsRequired { get; set; }
    public decimal MinimumScore { get; set; } = 0.8m;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}