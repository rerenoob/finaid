using System.ComponentModel.DataAnnotations;

namespace finaid.Models.Documents;

public class DocumentMetadata
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = null!;
    
    public long FileSizeBytes { get; set; }
    
    public Document.DocumentType Type { get; set; }
    
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? VirusScanResult { get; set; }
    
    public bool IsEncrypted { get; set; } = true;
    
    [MaxLength(500)]
    public string? BlobPath { get; set; }
    
    [MaxLength(64)]
    public string? FileHash { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
}

public enum DocumentStatus
{
    Uploaded = 0,
    Scanning = 1,
    Clean = 2,
    Quarantined = 3,
    Processing = 4,
    Verified = 5,
    Rejected = 6,
    Expired = 7
}