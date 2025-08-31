using System.ComponentModel.DataAnnotations;
using finaid.Models;
using finaid.Models.Document;
using finaid.Models.Documents;
using DocumentStatus = finaid.Models.Documents.DocumentStatus;

namespace finaid.Data.Entities;

public class UserDocument : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public DocumentType DocumentType { get; set; }
    
    [Required]
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? BlobPath { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSizeBytes { get; set; }
    
    [MaxLength(64)]
    public string? FileHash { get; set; }
    
    public bool IsEncrypted { get; set; } = true;
    
    [MaxLength(100)]
    public string? VirusScanResult { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // OCR and processing data
    public string? ExtractedText { get; set; }
    public string? ExtractedDataJson { get; set; }
    public string? OCRJobId { get; set; }
    public Guid? OCRResultId { get; set; }
    public decimal? OCRConfidence { get; set; }
    public decimal? ClassificationConfidence { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
    public string? ProcessingError { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? NextRetryAt { get; set; }
    
    // Verification information
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
    
    [MaxLength(500)]
    public string? ProcessingNotes { get; set; }
    
    // Expiration tracking
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    
    // Navigation properties - TODO: Fix relationships with User and ApplicationDocument
    // public Models.User.User User { get; set; } = null!;
    // public ICollection<ApplicationDocument> ApplicationDocuments { get; set; } = new List<ApplicationDocument>();
    public OCRProcessingResult? OCRResult { get; set; }
    
    // Convert to DocumentMetadata for storage operations
    public DocumentMetadata ToMetadata()
    {
        return new DocumentMetadata
        {
            Id = Id,
            UserId = UserId,
            FileName = FileName,
            ContentType = ContentType,
            FileSizeBytes = FileSizeBytes,
            Type = DocumentType,
            Status = Status,
            UploadedAt = UploadedAt,
            VirusScanResult = VirusScanResult,
            IsEncrypted = IsEncrypted,
            BlobPath = BlobPath,
            FileHash = FileHash,
            ExpiresAt = ExpiresAt
        };
    }
    
    // Update from DocumentMetadata
    public void UpdateFromMetadata(DocumentMetadata metadata)
    {
        FileName = metadata.FileName;
        ContentType = metadata.ContentType;
        FileSizeBytes = metadata.FileSizeBytes;
        DocumentType = metadata.Type;
        Status = metadata.Status;
        UploadedAt = metadata.UploadedAt;
        VirusScanResult = metadata.VirusScanResult;
        IsEncrypted = metadata.IsEncrypted;
        BlobPath = metadata.BlobPath;
        FileHash = metadata.FileHash;
        ExpiresAt = metadata.ExpiresAt;
    }
}