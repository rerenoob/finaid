using finaid.Models.Document;
using finaid.Models.OCR;

namespace finaid.Services.OCR;

public interface IOCRService
{
    /// <summary>
    /// Process a document using OCR and extract structured data
    /// </summary>
    Task<OCRResult> ProcessDocumentAsync(Guid documentId, DocumentType expectedType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Classify a document to determine its type
    /// </summary>
    Task<DocumentType> ClassifyDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extract structured data from a document using a specific template
    /// </summary>
    Task<Dictionary<string, object>> ExtractStructuredDataAsync(Guid documentId, string templateName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the overall confidence score for OCR processing of a document
    /// </summary>
    Task<decimal> GetConfidenceScoreAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the processing status of a document
    /// </summary>
    Task<ProcessingStatus> GetProcessingStatusAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retry failed OCR processing
    /// </summary>
    Task<OCRResult> RetryProcessingAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all available document templates
    /// </summary>
    Task<List<DocumentTemplate>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate extracted data against expected formats
    /// </summary>
    Task<ValidationResult> ValidateExtractedDataAsync(OCRResult ocrResult, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> CorrectedValues { get; set; } = new();
}