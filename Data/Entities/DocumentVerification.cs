using finaid.Models.Documents;
using finaid.Services.Documents;
using System.ComponentModel.DataAnnotations;

namespace finaid.Data.Entities;

public class DocumentVerification
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid DocumentId { get; set; }
    
    [Required]
    public string UserId { get; set; } = null!;
    
    public DocumentVerificationStatus Status { get; set; }
    
    public VerificationType VerificationType { get; set; }
    
    public decimal OverallScore { get; set; }
    
    public string? VerifierUserId { get; set; }
    
    public string? VerifierNotes { get; set; }
    
    public string? RejectionReason { get; set; }
    
    public string RequiredCorrections { get; set; } = "[]"; // JSON array
    
    public string Issues { get; set; } = "[]"; // JSON array
    
    public string VerificationChecks { get; set; } = "[]"; // JSON array
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? VerifiedAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public virtual UserDocument Document { get; set; } = null!;
}

// Extension method to convert to VerificationResult
public static class DocumentVerificationExtensions
{
    public static VerificationResult ToVerificationResult(this DocumentVerification entity)
    {
        return new VerificationResult
        {
            DocumentId = entity.DocumentId,
            Status = entity.Status,
            OverallScore = entity.OverallScore,
            VerifiedAt = entity.VerifiedAt ?? entity.CreatedAt,
            RequiresManualReview = entity.Status == DocumentVerificationStatus.ManualReviewRequired,
            VerifierNotes = entity.VerifierNotes,
            VerifierUserId = entity.VerifierUserId,
            Issues = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.Issues) ?? new(),
            RequiredCorrections = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.RequiredCorrections) ?? new(),
            Checks = System.Text.Json.JsonSerializer.Deserialize<List<VerificationCheck>>(entity.VerificationChecks) ?? new()
        };
    }
    
    public static DocumentVerification ToEntity(this VerificationResult result, string userId)
    {
        return new DocumentVerification
        {
            Id = Guid.NewGuid(),
            DocumentId = result.DocumentId,
            UserId = userId,
            Status = result.Status,
            OverallScore = result.OverallScore,
            VerifierUserId = result.VerifierUserId,
            VerifierNotes = result.VerifierNotes,
            Issues = System.Text.Json.JsonSerializer.Serialize(result.Issues),
            RequiredCorrections = System.Text.Json.JsonSerializer.Serialize(result.RequiredCorrections),
            VerificationChecks = System.Text.Json.JsonSerializer.Serialize(result.Checks),
            CreatedAt = DateTime.UtcNow,
            VerifiedAt = result.Status == DocumentVerificationStatus.Approved || result.Status == DocumentVerificationStatus.Rejected 
                ? DateTime.UtcNow 
                : null,
            ExpiresAt = DateTime.UtcNow.AddDays(30) // 30 days to complete verification
        };
    }
}