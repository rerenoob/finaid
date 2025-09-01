using finaid.Models.Documents;
using finaid.Models.Document;

namespace finaid.Services.Documents;

public interface IDocumentVerificationService
{
    Task<VerificationResult> VerifyDocumentAsync(Guid documentId, VerificationType type);
    Task<List<VerificationRule>> GetVerificationRulesAsync(DocumentType documentType);
    Task<bool> ApproveDocumentAsync(Guid documentId, string approverUserId, string notes);
    Task<bool> RejectDocumentAsync(Guid documentId, string rejectionReason, List<string> requiredCorrections);
    Task<DocumentVerificationStatus> GetVerificationStatusAsync(Guid documentId);
    Task<List<DocumentMetadata>> GetDocumentsRequiringReviewAsync();
    Task<bool> RequestManualReviewAsync(Guid documentId, string reason);
}

public enum VerificationType
{
    Automatic,
    Manual,
    Compliance,
    Identity,
    Financial
}

public enum VerificationCheckType
{
    ContentValidation,
    FormatCheck,
    DataConsistency,
    ComplianceCheck,
    IdentityVerification,
    FinancialValidation,
    DateRangeCheck,
    RequiredFieldCheck
}