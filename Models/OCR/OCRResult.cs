using finaid.Models.Document;

namespace finaid.Models.OCR;

public class OCRResult
{
    public Guid DocumentId { get; set; }
    public DocumentType ClassifiedType { get; set; }
    public decimal OverallConfidence { get; set; }
    public List<ExtractedField> Fields { get; set; } = new();
    public string RawText { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public List<string> ValidationErrors { get; set; } = new();
    public string? ProcessingJobId { get; set; }
}

public enum ProcessingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    RequiresReview = 4
}