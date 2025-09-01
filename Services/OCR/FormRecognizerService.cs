using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using finaid.Models.Document;
using finaid.Models.OCR;
using finaid.Services.Storage;
using finaid.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace finaid.Services.OCR;

public class FormRecognizerService : IOCRService
{
    private readonly DocumentAnalysisClient _documentAnalysisClient;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly FormRecognizerSettings _settings;
    private readonly ILogger<FormRecognizerService> _logger;
    private readonly Dictionary<DocumentType, string> _prebuiltModels;

    public FormRecognizerService(
        DocumentAnalysisClient documentAnalysisClient,
        IDocumentStorageService documentStorageService,
        IOptions<FormRecognizerSettings> settings,
        ILogger<FormRecognizerService> logger)
    {
        _documentAnalysisClient = documentAnalysisClient;
        _documentStorageService = documentStorageService;
        _settings = settings.Value;
        _logger = logger;
        
        _prebuiltModels = new Dictionary<DocumentType, string>
        {
            { DocumentType.TaxReturn, "prebuilt-tax.us.1040" },
            { DocumentType.W2Form, "prebuilt-tax.us.w2" },
            { DocumentType.BankStatement, "prebuilt-document" },
            { DocumentType.DriversLicense, "prebuilt-idDocument" },
            { DocumentType.Passport, "prebuilt-idDocument" }
        };
    }

    public async Task<OCRResult> ProcessDocumentAsync(Guid documentId, DocumentType expectedType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting OCR processing for document {DocumentId} of type {DocumentType}", 
                documentId, expectedType);

            var documentStream = await _documentStorageService.DownloadDocumentAsync(documentId, cancellationToken);
            var metadata = await _documentStorageService.GetDocumentMetadataAsync(documentId, cancellationToken);
            
            if (metadata == null)
            {
                throw new InvalidOperationException($"Document metadata not found for {documentId}");
            }

            var modelId = GetModelForDocumentType(expectedType);
            var operation = await _documentAnalysisClient.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                modelId,
                documentStream,
                cancellationToken: cancellationToken);

            var result = await operation.WaitForCompletionAsync(cancellationToken);
            var analyzedDocument = result.Value;

            var ocrResult = new OCRResult
            {
                DocumentId = documentId,
                ClassifiedType = expectedType,
                ProcessedAt = DateTime.UtcNow,
                Status = ProcessingStatus.Completed
            };

            // Extract text content
            ocrResult.RawText = analyzedDocument.Content ?? string.Empty;

            // Calculate overall confidence
            var confidenceScores = new List<float>();
            
            // Process documents and extract fields
            foreach (var document in analyzedDocument.Documents)
            {
                confidenceScores.Add(document.Confidence);
                
                foreach (var field in document.Fields)
                {
                    var extractedField = ConvertToExtractedField(field.Key, field.Value);
                    if (extractedField != null)
                    {
                        ocrResult.Fields.Add(extractedField);
                    }
                }
            }

            // Process key-value pairs if available
            foreach (var kvp in analyzedDocument.KeyValuePairs)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    var field = new ExtractedField
                    {
                        FieldName = kvp.Key.Content ?? "unknown",
                        Value = kvp.Value.Content,
                        Confidence = (decimal)kvp.Confidence,
                        DataType = DataTypes.Text,
                        RequiresValidation = kvp.Confidence < _settings.ConfidenceThreshold
                    };
                    
                    ocrResult.Fields.Add(field);
                    confidenceScores.Add(kvp.Confidence);
                }
            }

            // Calculate overall confidence
            ocrResult.OverallConfidence = confidenceScores.Any() 
                ? (decimal)confidenceScores.Average() 
                : 0m;

            // Validate results
            var validation = await ValidateExtractedDataAsync(ocrResult, cancellationToken);
            ocrResult.ValidationErrors = validation.Errors;
            
            if (ocrResult.OverallConfidence < (decimal)_settings.ConfidenceThreshold)
            {
                ocrResult.Status = ProcessingStatus.RequiresReview;
                _logger.LogWarning("Document {DocumentId} requires manual review due to low confidence: {Confidence}", 
                    documentId, ocrResult.OverallConfidence);
            }

            _logger.LogInformation("OCR processing completed for document {DocumentId} with confidence {Confidence}", 
                documentId, ocrResult.OverallConfidence);

            return ocrResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR processing failed for document {DocumentId}", documentId);
            return new OCRResult
            {
                DocumentId = documentId,
                Status = ProcessingStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                ValidationErrors = { ex.Message }
            };
        }
    }

    public async Task<DocumentType> ClassifyDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentStream = await _documentStorageService.DownloadDocumentAsync(documentId, cancellationToken);
            
            var operation = await _documentAnalysisClient.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-document",
                documentStream,
                cancellationToken: cancellationToken);

            var result = await operation.WaitForCompletionAsync(cancellationToken);
            var analyzedDocument = result.Value;

            // Simple classification based on content keywords
            var content = analyzedDocument.Content?.ToLowerInvariant() ?? string.Empty;
            
            if (content.Contains("form 1040") || content.Contains("tax return"))
                return DocumentType.TaxReturn;
            if (content.Contains("form w-2") || content.Contains("wage and tax statement"))
                return DocumentType.W2Form;
            if (content.Contains("bank statement") || content.Contains("account summary"))
                return DocumentType.BankStatement;
            if (content.Contains("driver") && content.Contains("license"))
                return DocumentType.DriversLicense;
            if (content.Contains("passport"))
                return DocumentType.Passport;
            if (content.Contains("transcript"))
            {
                if (content.Contains("high school"))
                    return DocumentType.HighSchoolTranscript;
                return DocumentType.CollegeTranscript;
            }

            return DocumentType.Other;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document classification failed for {DocumentId}", documentId);
            return DocumentType.Other;
        }
    }

    public async Task<Dictionary<string, object>> ExtractStructuredDataAsync(Guid documentId, string templateName, CancellationToken cancellationToken = default)
    {
        var ocrResult = await ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken);
        
        var structuredData = new Dictionary<string, object>();
        foreach (var field in ocrResult.Fields)
        {
            structuredData[field.FieldName] = field.Value ?? string.Empty;
        }
        
        return structuredData;
    }

    public async Task<decimal> GetConfidenceScoreAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // This would typically be cached or retrieved from a database
        // For now, we'll process the document to get the confidence
        var result = await ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken);
        return result.OverallConfidence;
    }

    public Task<ProcessingStatus> GetProcessingStatusAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // This would be retrieved from a processing status database/cache
        // For now, return pending as placeholder
        return Task.FromResult(ProcessingStatus.Pending);
    }

    public async Task<OCRResult> RetryProcessingAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrying OCR processing for document {DocumentId}", documentId);
        
        // Determine document type first
        var documentType = await ClassifyDocumentAsync(documentId, cancellationToken);
        return await ProcessDocumentAsync(documentId, documentType, cancellationToken);
    }

    public Task<List<DocumentTemplate>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
    {
        // This would typically come from a database or configuration
        // For now, return basic templates based on supported document types
        return Task.FromResult(new List<DocumentTemplate>
        {
            new DocumentTemplate
            {
                Name = "Tax Return (1040)",
                DocumentType = DocumentType.TaxReturn,
                Description = "Standard IRS Form 1040 tax return",
                Fields = GetTaxReturnFields()
            },
            new DocumentTemplate
            {
                Name = "W-2 Form",
                DocumentType = DocumentType.W2Form,
                Description = "IRS Form W-2 Wage and Tax Statement",
                Fields = GetW2Fields()
            }
        });
    }

    public Task<ValidationResult> ValidateExtractedDataAsync(OCRResult ocrResult, CancellationToken cancellationToken = default)
    {
        var validationResult = new ValidationResult { IsValid = true };

        foreach (var field in ocrResult.Fields)
        {
            try
            {
                var validationError = ValidateField(field);
                if (!string.IsNullOrEmpty(validationError))
                {
                    validationResult.Errors.Add($"{field.FieldName}: {validationError}");
                    validationResult.IsValid = false;
                }
            }
            catch (Exception ex)
            {
                validationResult.Warnings.Add($"Validation failed for {field.FieldName}: {ex.Message}");
            }
        }

        return Task.FromResult(validationResult);
    }

    private string GetModelForDocumentType(DocumentType documentType)
    {
        return _prebuiltModels.TryGetValue(documentType, out var modelId) 
            ? modelId 
            : "prebuilt-document";
    }

    private ExtractedField? ConvertToExtractedField(string fieldName, DocumentField documentField)
    {
        if (documentField.Content == null) return null;

        var dataType = documentField.FieldType switch
        {
            DocumentFieldType.Currency => DataTypes.Currency,
            DocumentFieldType.Date => DataTypes.Date,
            DocumentFieldType.Int64 => DataTypes.Number,
            DocumentFieldType.PhoneNumber => DataTypes.Phone,
            DocumentFieldType.Boolean => DataTypes.Boolean,
            _ => DataTypes.Text
        };

        object? value = documentField.FieldType switch
        {
            DocumentFieldType.Currency => documentField.Value.AsCurrency(),
            DocumentFieldType.Date => documentField.Value.AsDate(),
            DocumentFieldType.Int64 => documentField.Value.AsInt64(),
            DocumentFieldType.Double => documentField.Value.AsDouble(),
            DocumentFieldType.Boolean => documentField.Value.AsBoolean(),
            _ => documentField.Content
        };

        return new ExtractedField
        {
            FieldName = fieldName,
            Value = value,
            Confidence = (decimal)(documentField.Confidence ?? 0),
            DataType = dataType,
            RequiresValidation = (documentField.Confidence ?? 0) < _settings.ConfidenceThreshold
        };
    }

    private string? ValidateField(ExtractedField field)
    {
        if (field.Value == null) return null;

        return field.DataType switch
        {
            DataTypes.Currency => ValidateCurrency(field.Value),
            DataTypes.Date => ValidateDate(field.Value),
            DataTypes.Number => ValidateNumber(field.Value),
            DataTypes.SSN => ValidateSSN(field.Value.ToString()),
            DataTypes.Email => ValidateEmail(field.Value.ToString()),
            _ => null
        };
    }

    private string? ValidateCurrency(object value)
    {
        if (value is decimal || value is double || value is float)
            return null;
        
        if (decimal.TryParse(value.ToString(), out _))
            return null;
            
        return "Invalid currency format";
    }

    private string? ValidateDate(object value)
    {
        if (value is DateTime)
            return null;
            
        if (DateTime.TryParse(value.ToString(), out _))
            return null;
            
        return "Invalid date format";
    }

    private string? ValidateNumber(object value)
    {
        if (value is int || value is long || value is double || value is decimal)
            return null;
            
        if (double.TryParse(value.ToString(), out _))
            return null;
            
        return "Invalid number format";
    }

    private string? ValidateSSN(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
            
        var cleaned = value.Replace("-", "").Replace(" ", "");
        if (cleaned.Length == 9 && cleaned.All(char.IsDigit))
            return null;
            
        return "Invalid SSN format";
    }

    private string? ValidateEmail(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
            
        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == value ? null : "Invalid email format";
        }
        catch
        {
            return "Invalid email format";
        }
    }

    private List<TemplateField> GetTaxReturnFields()
    {
        return new List<TemplateField>
        {
            new() { Name = "FirstName", DisplayName = "First Name", DataType = DataTypes.Text, IsRequired = true },
            new() { Name = "LastName", DisplayName = "Last Name", DataType = DataTypes.Text, IsRequired = true },
            new() { Name = "SSN", DisplayName = "Social Security Number", DataType = DataTypes.SSN, IsRequired = true },
            new() { Name = "FilingStatus", DisplayName = "Filing Status", DataType = DataTypes.Text, IsRequired = true },
            new() { Name = "AGI", DisplayName = "Adjusted Gross Income", DataType = DataTypes.Currency, IsRequired = true }
        };
    }

    private List<TemplateField> GetW2Fields()
    {
        return new List<TemplateField>
        {
            new() { Name = "EmployeeName", DisplayName = "Employee Name", DataType = DataTypes.Text, IsRequired = true },
            new() { Name = "EmployeeSSN", DisplayName = "Employee SSN", DataType = DataTypes.SSN, IsRequired = true },
            new() { Name = "EmployerName", DisplayName = "Employer Name", DataType = DataTypes.Text, IsRequired = true },
            new() { Name = "WagesAmount", DisplayName = "Wages Amount", DataType = DataTypes.Currency, IsRequired = true },
            new() { Name = "FederalTaxWithheld", DisplayName = "Federal Tax Withheld", DataType = DataTypes.Currency, IsRequired = true }
        };
    }
}