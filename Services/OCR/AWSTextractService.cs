using Amazon.Textract;
using Amazon.Textract.Model;
using finaid.Configuration;
using finaid.Models.Document;
using finaid.Models.OCR;
using finaid.Services.Storage;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace finaid.Services.OCR;

public class AWSTextractService : IOCRService
{
    private readonly AmazonTextractClient _textractClient;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly AWSTextractSettings _settings;
    private readonly ILogger<AWSTextractService> _logger;
    private readonly Dictionary<DocumentType, FeatureType> _featureMappings;

    public AWSTextractService(
        AmazonTextractClient textractClient,
        IDocumentStorageService documentStorageService,
        IOptions<AWSTextractSettings> settings,
        ILogger<AWSTextractService> logger)
    {
        _textractClient = textractClient;
        _documentStorageService = documentStorageService;
        _settings = settings.Value;
        _logger = logger;
        
        _featureMappings = new Dictionary<DocumentType, FeatureType>
        {
            { DocumentType.TaxReturn, FeatureType.FORMS },
            { DocumentType.W2Form, FeatureType.FORMS },
            { DocumentType.BankStatement, FeatureType.TABLES },
            { DocumentType.DriversLicense, FeatureType.FORMS },
            { DocumentType.Passport, FeatureType.FORMS }
        };
    }

    public async Task<OCRResult> ProcessDocumentAsync(Guid documentId, DocumentType expectedType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Textract processing for document {DocumentId} of type {DocumentType}", 
                documentId, expectedType);

            var documentStream = await _documentStorageService.DownloadDocumentAsync(documentId, cancellationToken);
            var metadata = await _documentStorageService.GetDocumentMetadataAsync(documentId, cancellationToken);
            
            if (metadata == null)
            {
                throw new InvalidOperationException($"Document metadata not found for {documentId}");
            }

            var featureTypes = GetFeatureTypesForDocumentType(expectedType);
            var request = CreateAnalyzeDocumentRequest(documentStream, featureTypes);

            var response = await _textractClient.AnalyzeDocumentAsync(request, cancellationToken);

            var ocrResult = new OCRResult
            {
                DocumentId = documentId,
                ClassifiedType = expectedType,
                ProcessedAt = DateTime.UtcNow,
                Status = ProcessingStatus.Completed
            };

            // Extract text content
            ocrResult.RawText = ExtractTextFromResponse(response);

            // Extract fields and calculate confidence
            var confidenceScores = new List<float>();
            
            // Process forms
            foreach (var block in response.Blocks)
            {
                if (block.BlockType == BlockType.KEY_VALUE_SET && block.EntityTypes?.Contains("KEY") == true)
                {
                    var field = ExtractFieldFromBlock(block, response.Blocks);
                    if (field != null)
                    {
                        ocrResult.Fields.Add(field);
                        confidenceScores.Add((float)field.Confidence);
                    }
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

            _logger.LogInformation("Textract processing completed for document {DocumentId} with confidence {Confidence}", 
                documentId, ocrResult.OverallConfidence);

            return ocrResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Textract processing failed for document {DocumentId}", documentId);
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
            
            var request = new DetectDocumentTextRequest
            {
                Document = new Document
                {
                    Bytes = new MemoryStream(await ReadStreamToBytesAsync(documentStream))
                }
            };

            var response = await _textractClient.DetectDocumentTextAsync(request, cancellationToken);

            // Simple classification based on content keywords
            var content = ExtractTextFromResponse(response);
            
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
        var result = await ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken);
        return result.OverallConfidence;
    }

    public Task<ProcessingStatus> GetProcessingStatusAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProcessingStatus.Pending);
    }

    public async Task<OCRResult> RetryProcessingAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrying Textract processing for document {DocumentId}", documentId);
        
        var documentType = await ClassifyDocumentAsync(documentId, cancellationToken);
        return await ProcessDocumentAsync(documentId, documentType, cancellationToken);
    }

    public Task<List<DocumentTemplate>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
    {
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

    private List<FeatureType> GetFeatureTypesForDocumentType(DocumentType documentType)
    {
        var features = new List<FeatureType> { FeatureType.FORMS };
        
        if (documentType == DocumentType.BankStatement)
        {
            features.Add(FeatureType.TABLES);
        }
        
        return features;
    }

    private AnalyzeDocumentRequest CreateAnalyzeDocumentRequest(Stream documentStream, List<FeatureType> featureTypes)
    {
        return new AnalyzeDocumentRequest
        {
            Document = new Document
            {
                Bytes = new MemoryStream(ReadStreamToBytesAsync(documentStream).Result)
            },
            FeatureTypes = featureTypes.Select(ft => ft.ToString()).ToList()
        };
    }

    private string ExtractTextFromResponse(AnalyzeDocumentResponse response)
    {
        var textBlocks = response.Blocks
            .Where(b => b.BlockType == BlockType.LINE || b.BlockType == BlockType.WORD)
            .OrderBy(b => b.Geometry?.BoundingBox?.Top ?? 0)
            .ThenBy(b => b.Geometry?.BoundingBox?.Left ?? 0);

        return string.Join(" ", textBlocks.Select(b => b.Text));
    }

    private string ExtractTextFromResponse(DetectDocumentTextResponse response)
    {
        var textBlocks = response.Blocks
            .Where(b => b.BlockType == BlockType.LINE || b.BlockType == BlockType.WORD)
            .OrderBy(b => b.Geometry?.BoundingBox?.Top ?? 0)
            .ThenBy(b => b.Geometry?.BoundingBox?.Left ?? 0);

        return string.Join(" ", textBlocks.Select(b => b.Text));
    }

    private ExtractedField? ExtractFieldFromBlock(Block keyBlock, List<Block> allBlocks)
    {
        var valueBlock = allBlocks.FirstOrDefault(b => 
            b.BlockType == BlockType.KEY_VALUE_SET && 
            b.EntityTypes?.Contains("VALUE") == true &&
            b.Relationships?.Any(r => r.Type == "CHILD" && r.Ids.Contains(keyBlock.Id)) == true);

        if (valueBlock == null) return null;

        var fieldName = keyBlock.Text ?? "unknown";
        var fieldValue = valueBlock.Text ?? string.Empty;

        return new ExtractedField
        {
            FieldName = fieldName,
            Value = fieldValue,
            Confidence = (decimal)keyBlock.Confidence,
            DataType = DataTypes.Text,
            RequiresValidation = keyBlock.Confidence < _settings.ConfidenceThreshold
        };
    }

    private async Task<byte[]> ReadStreamToBytesAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
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