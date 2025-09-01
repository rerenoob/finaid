using finaid.Data;
using finaid.Models.Documents;
using finaid.Models.Document;
using finaid.Models.OCR;
using finaid.Services.OCR;
using finaid.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using DocumentStatus = finaid.Models.Document.DocumentStatus;

namespace finaid.Services.Forms;

public class FormPrePopulationService : IFormPrePopulationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOCRService _ocrService;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly ILogger<FormPrePopulationService> _logger;
    
    private readonly Dictionary<string, List<FieldMapping>> _fieldMappings;
    private readonly List<DataTransformation> _dataTransformations;

    public FormPrePopulationService(
        ApplicationDbContext dbContext,
        IOCRService ocrService,
        IDocumentStorageService documentStorageService,
        ILogger<FormPrePopulationService> logger)
    {
        _dbContext = dbContext;
        _ocrService = ocrService;
        _documentStorageService = documentStorageService;
        _logger = logger;
        
        _fieldMappings = InitializeFieldMappings();
        _dataTransformations = InitializeDataTransformations();
    }

    public async Task<PrePopulationResult> PrePopulateFromDocumentsAsync(Guid userId, string formType)
    {
        try
        {
            _logger.LogInformation("Starting form pre-population for user {UserId} and form type {FormType}", 
                userId, formType);

            var result = new PrePopulationResult
            {
                ProcessedAt = DateTime.UtcNow
            };

            // Get user's verified documents
            var userDocuments = await _dbContext.UserDocuments
                .Where(d => d.UserId == userId && d.Status == DocumentStatus.Verified)
                .ToListAsync();

            if (!userDocuments.Any())
            {
                result.Success = false;
                result.ValidationWarnings.Add("No verified documents found for user");
                return result;
            }

            var allPopulatedFields = new Dictionary<string, object>();
            var fieldSources = new Dictionary<string, string>();
            var confidenceScores = new List<decimal>();
            var conflictingFields = new List<string>();

            // Process each document
            foreach (var document in userDocuments)
            {
                try
                {
                    // Get OCR results for the document
                    var ocrResult = await _ocrService.ProcessDocumentAsync(document.Id, document.DocumentType);
                    
                    if (ocrResult.Status != ProcessingStatus.Completed)
                    {
                        _logger.LogWarning("Skipping document {DocumentId} due to OCR status: {Status}", 
                            document.Id, ocrResult.Status);
                        continue;
                    }

                    // Map document data to form fields
                    var mappedData = await MapDocumentDataToFormAsync(ocrResult, formType);
                    
                    if (!mappedData.Any())
                    {
                        continue;
                    }

                    // Merge data, handling conflicts
                    foreach (var kvp in mappedData)
                    {
                        if (allPopulatedFields.ContainsKey(kvp.Key))
                        {
                            // Handle field conflict
                            var existingValue = allPopulatedFields[kvp.Key];
                            var newValue = kvp.Value;
                            
                            if (!AreValuesEqual(existingValue, newValue))
                            {
                                conflictingFields.Add(kvp.Key);
                                _logger.LogWarning("Field conflict for {FieldName}: existing='{ExistingValue}', new='{NewValue}'", 
                                    kvp.Key, existingValue, newValue);
                                
                                // Keep the value from the higher priority document (for now, keep existing)
                                continue;
                            }
                        }
                        
                        allPopulatedFields[kvp.Key] = kvp.Value;
                        fieldSources[kvp.Key] = document.FileName;
                    }

                    // Collect confidence scores
                    confidenceScores.AddRange(ocrResult.Fields.Select(f => f.Confidence));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process document {DocumentId} for pre-population", document.Id);
                    result.ValidationWarnings.Add($"Failed to process document: {document.FileName}");
                }
            }

            // Set result properties
            result.Success = allPopulatedFields.Any();
            result.PopulatedFields = allPopulatedFields;
            result.ConflictingFields = conflictingFields;
            result.FieldSources = fieldSources;
            result.OverallConfidence = confidenceScores.Any() ? confidenceScores.Average() : 0m;

            // Validate pre-populated data
            var isValid = await ValidatePrePopulatedDataAsync(allPopulatedFields, formType);
            if (!isValid)
            {
                result.ValidationWarnings.Add("Some pre-populated data failed validation checks");
            }

            // Save pre-populated data
            await SavePrePopulatedDataAsync(userId, formType, allPopulatedFields);

            _logger.LogInformation("Form pre-population completed for user {UserId}: {FieldCount} fields populated with confidence {Confidence}", 
                userId, allPopulatedFields.Count, result.OverallConfidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Form pre-population failed for user {UserId} and form type {FormType}", 
                userId, formType);
            
            return new PrePopulationResult
            {
                Success = false,
                ProcessedAt = DateTime.UtcNow,
                ValidationWarnings = { $"Pre-population failed: {ex.Message}" }
            };
        }
    }

    public async Task<Dictionary<string, object>> MapDocumentDataToFormAsync(OCRResult ocrResult, string targetForm)
    {
        var mappedData = new Dictionary<string, object>();
        
        if (!ocrResult.Fields.Any())
        {
            return mappedData;
        }

        // Get field mappings for this form
        var mappingKey = $"{ocrResult.ClassifiedType}_{targetForm}";
        if (!_fieldMappings.TryGetValue(mappingKey, out var mappings))
        {
            // Try generic mappings
            mappingKey = $"{DocumentType.Other}_{targetForm}";
            if (!_fieldMappings.TryGetValue(mappingKey, out mappings))
            {
                return mappedData;
            }
        }

        foreach (var mapping in mappings)
        {
            try
            {
                // Find matching OCR field
                var ocrField = ocrResult.Fields.FirstOrDefault(f => 
                    f.FieldName.Contains(mapping.SourceField, StringComparison.OrdinalIgnoreCase) ||
                    mapping.SourceField.Contains(f.FieldName, StringComparison.OrdinalIgnoreCase));

                if (ocrField == null || ocrField.Value == null)
                {
                    continue;
                }

                // Check confidence threshold
                if (ocrField.Confidence < mapping.MinConfidence)
                {
                    _logger.LogDebug("Skipping field {FieldName} due to low confidence: {Confidence} < {MinConfidence}",
                        mapping.SourceField, ocrField.Confidence, mapping.MinConfidence);
                    continue;
                }

                // Apply data transformations
                var transformedValue = await ApplyTransformationsAsync(ocrField.Value, mapping);
                
                if (transformedValue != null)
                {
                    mappedData[mapping.TargetField] = transformedValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map field {SourceField} to {TargetField}", 
                    mapping.SourceField, mapping.TargetField);
            }
        }

        return mappedData;
    }

    public Task<List<FieldMapping>> GetFieldMappingsAsync(DocumentType documentType, string targetForm)
    {
        var mappingKey = $"{documentType}_{targetForm}";
        return Task.FromResult(_fieldMappings.TryGetValue(mappingKey, out var mappings) 
            ? mappings 
            : new List<FieldMapping>());
    }

    public Task<bool> ValidatePrePopulatedDataAsync(Dictionary<string, object> formData, string formType)
    {
        var isValid = true;

        foreach (var kvp in formData)
        {
            try
            {
                var fieldName = kvp.Key;
                var value = kvp.Value;

                // Basic validation based on field name patterns
                if (fieldName.ToLower().Contains("ssn"))
                {
                    if (!IsValidSSN(value?.ToString()))
                    {
                        _logger.LogWarning("Invalid SSN format in field {FieldName}: {Value}", fieldName, value);
                        isValid = false;
                    }
                }
                else if (fieldName.ToLower().Contains("email"))
                {
                    if (!IsValidEmail(value?.ToString()))
                    {
                        _logger.LogWarning("Invalid email format in field {FieldName}: {Value}", fieldName, value);
                        isValid = false;
                    }
                }
                else if (fieldName.ToLower().Contains("income") || fieldName.ToLower().Contains("amount"))
                {
                    if (!IsValidCurrency(value))
                    {
                        _logger.LogWarning("Invalid currency format in field {FieldName}: {Value}", fieldName, value);
                        isValid = false;
                    }
                }
                else if (fieldName.ToLower().Contains("date"))
                {
                    if (!IsValidDate(value))
                    {
                        _logger.LogWarning("Invalid date format in field {FieldName}: {Value}", fieldName, value);
                        isValid = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation failed for field {FieldName}", kvp.Key);
                isValid = false;
            }
        }

        return Task.FromResult(isValid);
    }

    public Task SavePrePopulatedDataAsync(Guid userId, string formType, Dictionary<string, object> data)
    {
        try
        {
            var dataJson = JsonSerializer.Serialize(data);
            
            // In a real implementation, this would save to a PrePopulatedFormData table
            // For now, we'll just log it
            _logger.LogInformation("Saving pre-populated data for user {UserId}, form {FormType}: {DataJson}", 
                userId, formType, dataJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save pre-populated data for user {UserId}", userId);
        }
        return Task.CompletedTask;
    }

    public Task<PrePopulationResult> GetSavedPrePopulationAsync(Guid userId, string formType)
    {
        try
        {
            // In a real implementation, this would retrieve from database
            // For now, return an empty result
            return Task.FromResult(new PrePopulationResult
            {
                Success = false,
                ProcessedAt = DateTime.UtcNow,
                ValidationWarnings = { "Saved pre-population data not implemented yet" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve saved pre-population data for user {UserId}", userId);
            
            return Task.FromResult(new PrePopulationResult
            {
                Success = false,
                ProcessedAt = DateTime.UtcNow,
                ValidationWarnings = { $"Failed to retrieve data: {ex.Message}" }
            });
        }
    }

    public Task<List<DataTransformation>> GetAvailableTransformationsAsync()
    {
        return Task.FromResult(_dataTransformations);
    }

    private Task<object?> ApplyTransformationsAsync(object value, FieldMapping mapping)
    {
        if (value == null || !mapping.TransformationRules.Any())
        {
            return Task.FromResult<object?>(value);
        }

        var currentValue = value;

        foreach (var transformationName in mapping.TransformationRules)
        {
            var transformation = _dataTransformations.FirstOrDefault(t => t.TransformationName == transformationName);
            if (transformation != null)
            {
                try
                {
                    currentValue = transformation.TransformFunction(currentValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply transformation {TransformationName} to value {Value}", 
                        transformationName, currentValue);
                    break;
                }
            }
        }

        return Task.FromResult<object?>(currentValue);
    }

    private bool AreValuesEqual(object value1, object value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        
        // Normalize strings for comparison
        if (value1 is string str1 && value2 is string str2)
        {
            return string.Equals(str1.Trim(), str2.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        
        return value1.Equals(value2);
    }

    private bool IsValidSSN(string? ssn)
    {
        if (string.IsNullOrEmpty(ssn)) return false;
        
        var cleaned = ssn.Replace("-", "").Replace(" ", "");
        return cleaned.Length == 9 && cleaned.All(char.IsDigit);
    }

    private bool IsValidEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidCurrency(object? value)
    {
        if (value == null) return false;
        
        return value switch
        {
            decimal _ => true,
            double _ => true,
            float _ => true,
            string str => decimal.TryParse(str.Replace("$", "").Replace(",", ""), out _),
            _ => false
        };
    }

    private bool IsValidDate(object? value)
    {
        if (value == null) return false;
        
        return value switch
        {
            DateTime _ => true,
            string str => DateTime.TryParse(str, out _),
            _ => false
        };
    }

    private Dictionary<string, List<FieldMapping>> InitializeFieldMappings()
    {
        var mappings = new Dictionary<string, List<FieldMapping>>();

        // FAFSA Form Mappings
        var fafsaMappings = new List<FieldMapping>
        {
            // Tax Return to FAFSA mappings
            new() 
            { 
                SourceField = "FirstName", 
                TargetField = "student_first_name", 
                DataType = DataTypes.Text,
                MinConfidence = 0.8m,
                Priority = 1
            },
            new() 
            { 
                SourceField = "LastName", 
                TargetField = "student_last_name", 
                DataType = DataTypes.Text,
                MinConfidence = 0.8m,
                Priority = 1
            },
            new() 
            { 
                SourceField = "SSN", 
                TargetField = "student_ssn", 
                DataType = DataTypes.SSN,
                MinConfidence = 0.9m,
                Priority = 1,
                TransformationRules = { "FormatSSN" }
            },
            new() 
            { 
                SourceField = "AGI", 
                TargetField = "student_agi", 
                DataType = DataTypes.Currency,
                MinConfidence = 0.85m,
                Priority = 1,
                TransformationRules = { "ParseCurrency" }
            },
            new() 
            { 
                SourceField = "FilingStatus", 
                TargetField = "student_tax_filing_status", 
                DataType = DataTypes.Text,
                MinConfidence = 0.8m,
                Priority = 1
            }
        };

        mappings[$"{DocumentType.TaxReturn}_FAFSA"] = fafsaMappings;

        // W-2 to FAFSA mappings
        var w2ToFafsaMappings = new List<FieldMapping>
        {
            new() 
            { 
                SourceField = "EmployeeName", 
                TargetField = "student_first_name", 
                DataType = DataTypes.Text,
                MinConfidence = 0.8m,
                Priority = 2, // Lower priority than tax return
                TransformationRules = { "ExtractFirstName" }
            },
            new() 
            { 
                SourceField = "EmployeeName", 
                TargetField = "student_last_name", 
                DataType = DataTypes.Text,
                MinConfidence = 0.8m,
                Priority = 2,
                TransformationRules = { "ExtractLastName" }
            },
            new() 
            { 
                SourceField = "EmployeeSSN", 
                TargetField = "student_ssn", 
                DataType = DataTypes.SSN,
                MinConfidence = 0.9m,
                Priority = 2,
                TransformationRules = { "FormatSSN" }
            },
            new() 
            { 
                SourceField = "WagesAmount", 
                TargetField = "student_income_work", 
                DataType = DataTypes.Currency,
                MinConfidence = 0.85m,
                Priority = 1,
                TransformationRules = { "ParseCurrency" }
            }
        };

        mappings[$"{DocumentType.W2Form}_FAFSA"] = w2ToFafsaMappings;

        return mappings;
    }

    private List<DataTransformation> InitializeDataTransformations()
    {
        return new List<DataTransformation>
        {
            new()
            {
                TransformationName = "FormatSSN",
                Description = "Format SSN with dashes (XXX-XX-XXXX)",
                SourceFormat = "Various SSN formats",
                TargetFormat = "XXX-XX-XXXX",
                TransformFunction = (value) =>
                {
                    var str = value?.ToString()?.Replace("-", "").Replace(" ", "");
                    if (str?.Length == 9 && str.All(char.IsDigit))
                    {
                        return $"{str.Substring(0, 3)}-{str.Substring(3, 2)}-{str.Substring(5, 4)}";
                    }
                    return value!;
                },
                IsReversible = true
            },
            new()
            {
                TransformationName = "ParseCurrency",
                Description = "Parse currency value to decimal",
                SourceFormat = "String with currency symbols",
                TargetFormat = "Decimal",
                TransformFunction = (value) =>
                {
                    if (value is decimal d) return d;
                    if (value is double db) return (decimal)db;
                    if (value is string str)
                    {
                        var cleaned = str.Replace("$", "").Replace(",", "").Trim();
                        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
                        {
                            return result;
                        }
                    }
                    return value!;
                },
                IsReversible = false
            },
            new()
            {
                TransformationName = "ExtractFirstName",
                Description = "Extract first name from full name",
                SourceFormat = "Full name string",
                TargetFormat = "First name",
                TransformFunction = (value) =>
                {
                    var str = value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(str))
                    {
                        var parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return parts.Length > 0 ? parts[0] : str;
                    }
                    return value!;
                },
                IsReversible = false
            },
            new()
            {
                TransformationName = "ExtractLastName",
                Description = "Extract last name from full name",
                SourceFormat = "Full name string",
                TargetFormat = "Last name",
                TransformFunction = (value) =>
                {
                    var str = value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(str))
                    {
                        var parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return parts.Length > 1 ? parts[^1] : str;
                    }
                    return value!;
                },
                IsReversible = false
            },
            new()
            {
                TransformationName = "NormalizeDate",
                Description = "Normalize date to ISO format",
                SourceFormat = "Various date formats",
                TargetFormat = "YYYY-MM-DD",
                TransformFunction = (value) =>
                {
                    if (value is DateTime dt) return dt.ToString("yyyy-MM-dd");
                    if (DateTime.TryParse(value?.ToString(), out var parsed))
                    {
                        return parsed.ToString("yyyy-MM-dd");
                    }
                    return value!;
                },
                IsReversible = true
            }
        };
    }
}