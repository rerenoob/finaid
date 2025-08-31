using System.ComponentModel.DataAnnotations;
using finaid.Models.User;

namespace finaid.Models.Document;

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
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [MaxLength(64)]
    public string? FileHash { get; set; }
    
    // OCR and processing data
    public string? ExtractedText { get; set; }
    public string? ExtractedDataJson { get; set; }
    
    // Verification information
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
    
    [MaxLength(500)]
    public string? ProcessingNotes { get; set; }
    
    // Expiration tracking
    public DateTime? ExpirationDate { get; set; }
    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
    
    // Navigation properties
    public User.User User { get; set; } = null!;
    public ICollection<ApplicationDocument> ApplicationDocuments { get; set; } = new List<ApplicationDocument>();
}