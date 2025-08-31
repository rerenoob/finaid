using finaid.Models.Document;
using finaid.Models.OCR;
using System.ComponentModel.DataAnnotations;

namespace finaid.Data.Entities;

public class OCRProcessingResult
{
    public Guid Id { get; set; }
    
    public Guid DocumentId { get; set; }
    
    public DateTime ProcessedAt { get; set; }
    
    [Range(0.0, 1.0)]
    public decimal OverallConfidence { get; set; }
    
    public string? RawText { get; set; }
    
    public string? ExtractedFields { get; set; } // JSON serialized ExtractedField[]
    
    public string? ValidationErrors { get; set; } // JSON serialized string[]
    
    public ProcessingStatus ProcessingStatus { get; set; }
    
    public DocumentType ClassifiedType { get; set; }
    
    public string? ProcessingJobId { get; set; }
    
    public int ProcessingTimeMs { get; set; }
    
    public string? ModelVersion { get; set; }
    
    // Navigation property
    public UserDocument Document { get; set; } = null!;
}