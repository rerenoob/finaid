using finaid.Models.Documents;
using finaid.Models.OCR;
using finaid.Models.Document;

namespace finaid.Services.Forms;

public interface IFormPrePopulationService
{
    Task<PrePopulationResult> PrePopulateFromDocumentsAsync(Guid userId, string formType);
    Task<Dictionary<string, object>> MapDocumentDataToFormAsync(OCRResult ocrResult, string targetForm);
    Task<List<FieldMapping>> GetFieldMappingsAsync(DocumentType documentType, string targetForm);
    Task<bool> ValidatePrePopulatedDataAsync(Dictionary<string, object> formData, string formType);
    Task SavePrePopulatedDataAsync(Guid userId, string formType, Dictionary<string, object> data);
    Task<PrePopulationResult> GetSavedPrePopulationAsync(Guid userId, string formType);
    Task<List<DataTransformation>> GetAvailableTransformationsAsync();
}

public class PrePopulationResult
{
    public bool Success { get; set; }
    public Dictionary<string, object> PopulatedFields { get; set; } = new();
    public List<string> ConflictingFields { get; set; } = new();
    public List<string> MissingFields { get; set; } = new();
    public decimal OverallConfidence { get; set; }
    public List<DataTransformation> TransformationsApplied { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public Dictionary<string, string> FieldSources { get; set; } = new(); // Field name -> Source document
    public List<string> ValidationWarnings { get; set; } = new();
}

public class FieldMapping
{
    public string SourceField { get; set; } = null!; // OCR field name
    public string TargetField { get; set; } = null!; // Form field name
    public string DataType { get; set; } = null!;
    public List<string> TransformationRules { get; set; } = new();
    public decimal MinConfidence { get; set; } = 0.8m;
    public bool RequiresValidation { get; set; }
    public string? ValidationPattern { get; set; }
    public int Priority { get; set; } = 1; // For handling conflicts
}

public class DataTransformation
{
    public string TransformationName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string SourceFormat { get; set; } = null!;
    public string TargetFormat { get; set; } = null!;
    public Func<object, object> TransformFunction { get; set; } = null!;
    public bool IsReversible { get; set; }
}