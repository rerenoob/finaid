using finaid.Data;
using finaid.Data.Entities;
using finaid.Models.Documents;
using finaid.Models.Document;
using finaid.Models.OCR;
using finaid.Services.OCR;
using finaid.Services.Storage;
using finaid.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DocumentStatus = finaid.Models.Document.DocumentStatus;

namespace finaid.Services.Documents;

public class DocumentVerificationService : IDocumentVerificationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOCRService _ocrService;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly IDocumentNotificationService _notificationService;
    private readonly ILogger<DocumentVerificationService> _logger;
    
    private readonly Dictionary<DocumentType, List<VerificationRule>> _verificationRules;

    public DocumentVerificationService(
        ApplicationDbContext dbContext,
        IOCRService ocrService,
        IDocumentStorageService documentStorageService,
        IDocumentNotificationService notificationService,
        ILogger<DocumentVerificationService> logger)
    {
        _dbContext = dbContext;
        _ocrService = ocrService;
        _documentStorageService = documentStorageService;
        _notificationService = notificationService;
        _logger = logger;
        
        _verificationRules = InitializeVerificationRules();
    }

    public async Task<VerificationResult> VerifyDocumentAsync(Guid documentId, VerificationType type)
    {
        try
        {
            _logger.LogInformation("Starting document verification for {DocumentId} with type {VerificationType}", 
                documentId, type);

            var document = await _dbContext.UserDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId);
                
            if (document == null)
            {
                throw new InvalidOperationException($"Document {documentId} not found");
            }

            var result = new VerificationResult
            {
                DocumentId = documentId,
                Status = DocumentVerificationStatus.InProgress,
                VerifiedAt = DateTime.UtcNow
            };

            // Get verification rules for this document type
            var rules = await GetVerificationRulesAsync(document.DocumentType);
            var checks = new List<VerificationCheck>();

            // Run OCR processing first if not already done
            var ocrResult = await _ocrService.ProcessDocumentAsync(documentId, document.DocumentType);
            
            // Perform verification checks based on rules
            foreach (var rule in rules.Where(r => r.IsEnabled))
            {
                var check = await PerformVerificationCheckAsync(rule, document, ocrResult);
                checks.Add(check);
            }

            result.Checks = checks;
            
            // Calculate overall score
            var passedChecks = checks.Where(c => c.Passed).ToList();
            var requiredChecks = checks.Where(c => rules.First(r => r.RuleName == c.CheckName).IsRequired).ToList();
            
            if (requiredChecks.Any() && requiredChecks.All(c => c.Passed))
            {
                result.OverallScore = (decimal)checks.Average(c => (double)c.Confidence);
            }
            else
            {
                result.OverallScore = 0m; // Fail if any required check fails
            }

            // Determine verification status
            result.Status = DetermineVerificationStatus(result, type);
            
            // Collect issues
            result.Issues = checks
                .Where(c => !c.Passed)
                .SelectMany(c => c.ValidationMessages)
                .ToList();

            // Save verification record
            await SaveVerificationResultAsync(result, document.UserId.ToString());
            
            // Send notifications if needed
            await SendVerificationNotificationAsync(result, document.UserId);

            _logger.LogInformation("Document verification completed for {DocumentId} with status {Status} and score {Score}", 
                documentId, result.Status, result.OverallScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document verification failed for {DocumentId}", documentId);
            
            var errorResult = new VerificationResult
            {
                DocumentId = documentId,
                Status = DocumentVerificationStatus.ManualReviewRequired,
                VerifiedAt = DateTime.UtcNow,
                Issues = { $"Verification failed: {ex.Message}" },
                RequiresManualReview = true
            };
            
            await SaveVerificationResultAsync(errorResult, "system");
            return errorResult;
        }
    }

    public Task<List<VerificationRule>> GetVerificationRulesAsync(DocumentType documentType)
    {
        return Task.FromResult(_verificationRules.TryGetValue(documentType, out var rules) 
            ? rules 
            : _verificationRules[DocumentType.Other]);
    }

    public async Task<bool> ApproveDocumentAsync(Guid documentId, string approverUserId, string notes)
    {
        try
        {
            var verification = await _dbContext.DocumentVerifications
                .FirstOrDefaultAsync(v => v.DocumentId == documentId);
                
            if (verification == null)
            {
                return false;
            }

            verification.Status = DocumentVerificationStatus.Approved;
            verification.VerifierUserId = approverUserId;
            verification.VerifierNotes = notes;
            verification.VerifiedAt = DateTime.UtcNow;

            // Update document status
            var document = await _dbContext.UserDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId);
                
            if (document != null)
            {
                document.Status = DocumentStatus.Verified;
            }

            await _dbContext.SaveChangesAsync();
            
            // Send notification
            await _notificationService.SendDocumentApprovedNotificationAsync(documentId, Guid.Parse(verification.UserId));
            
            _logger.LogInformation("Document {DocumentId} approved by {ApproverUserId}", documentId, approverUserId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> RejectDocumentAsync(Guid documentId, string rejectionReason, List<string> requiredCorrections)
    {
        try
        {
            var verification = await _dbContext.DocumentVerifications
                .FirstOrDefaultAsync(v => v.DocumentId == documentId);
                
            if (verification == null)
            {
                return false;
            }

            verification.Status = DocumentVerificationStatus.Rejected;
            verification.RejectionReason = rejectionReason;
            verification.RequiredCorrections = JsonSerializer.Serialize(requiredCorrections);
            verification.VerifiedAt = DateTime.UtcNow;

            // Update document status
            var document = await _dbContext.UserDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId);
                
            if (document != null)
            {
                document.Status = DocumentStatus.Rejected;
            }

            await _dbContext.SaveChangesAsync();
            
            // Send notification
            await _notificationService.SendDocumentRejectedNotificationAsync(
                documentId, 
                Guid.Parse(verification.UserId), 
                rejectionReason, 
                requiredCorrections);
            
            _logger.LogInformation("Document {DocumentId} rejected with reason: {RejectionReason}", 
                documentId, rejectionReason);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<DocumentVerificationStatus> GetVerificationStatusAsync(Guid documentId)
    {
        var verification = await _dbContext.DocumentVerifications
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();
            
        return verification?.Status ?? DocumentVerificationStatus.Pending;
    }

    public async Task<List<DocumentMetadata>> GetDocumentsRequiringReviewAsync()
    {
        var documentIds = await _dbContext.DocumentVerifications
            .Where(v => v.Status == DocumentVerificationStatus.ManualReviewRequired)
            .Select(v => v.DocumentId)
            .ToListAsync();
            
        var documents = new List<DocumentMetadata>();
        
        foreach (var docId in documentIds)
        {
            var metadata = await _documentStorageService.GetDocumentMetadataAsync(docId);
            if (metadata != null)
            {
                documents.Add(metadata);
            }
        }
        
        return documents;
    }

    public async Task<bool> RequestManualReviewAsync(Guid documentId, string reason)
    {
        try
        {
            var verification = await _dbContext.DocumentVerifications
                .FirstOrDefaultAsync(v => v.DocumentId == documentId);
                
            if (verification == null)
            {
                return false;
            }

            verification.Status = DocumentVerificationStatus.ManualReviewRequired;
            verification.VerifierNotes = reason;
            
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Manual review requested for document {DocumentId}: {Reason}", 
                documentId, reason);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request manual review for document {DocumentId}", documentId);
            return false;
        }
    }

    private async Task<VerificationCheck> PerformVerificationCheckAsync(
        VerificationRule rule, 
        Data.Entities.UserDocument document, 
        OCRResult ocrResult)
    {
        var check = new VerificationCheck
        {
            CheckName = rule.RuleName,
            Type = rule.CheckType,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            switch (rule.CheckType)
            {
                case VerificationCheckType.ContentValidation:
                    await PerformContentValidationAsync(check, ocrResult, rule);
                    break;
                    
                case VerificationCheckType.FormatCheck:
                    await PerformFormatCheckAsync(check, document, rule);
                    break;
                    
                case VerificationCheckType.DataConsistency:
                    await PerformDataConsistencyCheckAsync(check, ocrResult, rule);
                    break;
                    
                case VerificationCheckType.RequiredFieldCheck:
                    await PerformRequiredFieldCheckAsync(check, ocrResult, rule);
                    break;
                    
                case VerificationCheckType.DateRangeCheck:
                    await PerformDateRangeCheckAsync(check, ocrResult, rule);
                    break;
                    
                default:
                    check.Passed = true;
                    check.Confidence = 1.0m;
                    check.Details = "Check type not implemented yet";
                    break;
            }
        }
        catch (Exception ex)
        {
            check.Passed = false;
            check.Confidence = 0m;
            check.Details = $"Check failed: {ex.Message}";
            check.ValidationMessages.Add(ex.Message);
        }

        return check;
    }

    private Task PerformContentValidationAsync(VerificationCheck check, OCRResult ocrResult, VerificationRule rule)
    {
        // Validate that extracted content meets minimum confidence threshold
        var lowConfidenceFields = ocrResult.Fields.Where(f => f.Confidence < rule.MinimumScore).ToList();
        
        if (lowConfidenceFields.Any())
        {
            check.Passed = false;
            check.Confidence = lowConfidenceFields.Min(f => f.Confidence);
            check.Details = $"Low confidence fields: {string.Join(", ", lowConfidenceFields.Select(f => f.FieldName))}";
            check.ValidationMessages.Add($"Document contains {lowConfidenceFields.Count} fields with low confidence scores");
        }
        else
        {
            check.Passed = true;
            check.Confidence = ocrResult.Fields.Any() ? ocrResult.Fields.Average(f => f.Confidence) : 1.0m;
            check.Details = "All extracted fields meet confidence requirements";
        }
        return Task.CompletedTask;
    }

    private Task PerformFormatCheckAsync(VerificationCheck check, Data.Entities.UserDocument document, VerificationRule rule)
    {
        // Validate file format and basic document properties
        var allowedTypes = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
        
        if (!allowedTypes.Contains(extension))
        {
            check.Passed = false;
            check.Confidence = 0m;
            check.Details = $"Invalid file format: {extension}";
            check.ValidationMessages.Add($"File format {extension} is not supported");
        }
        else if (document.FileSizeBytes > 50 * 1024 * 1024) // 50MB limit
        {
            check.Passed = false;
            check.Confidence = 0m;
            check.Details = "File size exceeds limit";
            check.ValidationMessages.Add("File size exceeds 50MB limit");
        }
        else
        {
            check.Passed = true;
            check.Confidence = 1.0m;
            check.Details = "File format and size are valid";
        }
        return Task.CompletedTask;
    }

    private Task PerformDataConsistencyCheckAsync(VerificationCheck check, OCRResult ocrResult, VerificationRule rule)
    {
        // Check for data consistency within the document
        var inconsistencies = new List<string>();
        
        // Example: Check for consistent SSN format across fields
        var ssnFields = ocrResult.Fields.Where(f => f.DataType == DataTypes.SSN).ToList();
        if (ssnFields.Count > 1)
        {
            var ssnValues = ssnFields.Select(f => f.Value?.ToString()?.Replace("-", "").Replace(" ", "")).Distinct().ToList();
            if (ssnValues.Count > 1)
            {
                inconsistencies.Add("Multiple different SSN values found in document");
            }
        }
        
        // Example: Check date consistency
        var dateFields = ocrResult.Fields.Where(f => f.DataType == DataTypes.Date).ToList();
        foreach (var dateField in dateFields)
        {
            if (DateTime.TryParse(dateField.Value?.ToString(), out var date))
            {
                if (date.Year < 1900 || date.Year > DateTime.Now.Year + 1)
                {
                    inconsistencies.Add($"Suspicious date found: {dateField.FieldName} = {date:yyyy-MM-dd}");
                }
            }
        }

        if (inconsistencies.Any())
        {
            check.Passed = false;
            check.Confidence = 0.5m;
            check.Details = string.Join("; ", inconsistencies);
            check.ValidationMessages.AddRange(inconsistencies);
        }
        else
        {
            check.Passed = true;
            check.Confidence = 1.0m;
            check.Details = "Data consistency checks passed";
        }
        return Task.CompletedTask;
    }

    private Task PerformRequiredFieldCheckAsync(VerificationCheck check, OCRResult ocrResult, VerificationRule rule)
    {
        // Get required fields from rule parameters
        if (!rule.Parameters.TryGetValue("requiredFields", out var requiredFieldsObj) ||
            requiredFieldsObj is not List<string> requiredFields)
        {
            check.Passed = true;
            check.Confidence = 1.0m;
            check.Details = "No required fields specified";
            return Task.CompletedTask;
        }

        var extractedFieldNames = ocrResult.Fields.Select(f => f.FieldName).ToList();
        var missingFields = requiredFields.Where(rf => !extractedFieldNames.Any(ef => 
            ef.Contains(rf, StringComparison.OrdinalIgnoreCase))).ToList();

        if (missingFields.Any())
        {
            check.Passed = false;
            check.Confidence = (decimal)(requiredFields.Count - missingFields.Count) / requiredFields.Count;
            check.Details = $"Missing required fields: {string.Join(", ", missingFields)}";
            check.ValidationMessages.Add($"Document is missing {missingFields.Count} required fields");
        }
        else
        {
            check.Passed = true;
            check.Confidence = 1.0m;
            check.Details = "All required fields are present";
        }
        return Task.CompletedTask;
    }

    private Task PerformDateRangeCheckAsync(VerificationCheck check, OCRResult ocrResult, VerificationRule rule)
    {
        // Check if dates fall within acceptable ranges
        var dateFields = ocrResult.Fields.Where(f => f.DataType == DataTypes.Date).ToList();
        var invalidDates = new List<string>();

        foreach (var field in dateFields)
        {
            if (DateTime.TryParse(field.Value?.ToString(), out var date))
            {
                // Check if date is within reasonable range (e.g., tax year documents)
                var currentYear = DateTime.Now.Year;
                if (date.Year < currentYear - 7 || date.Year > currentYear)
                {
                    invalidDates.Add($"{field.FieldName}: {date:yyyy-MM-dd}");
                }
            }
        }

        if (invalidDates.Any())
        {
            check.Passed = false;
            check.Confidence = 0.3m;
            check.Details = $"Dates outside expected range: {string.Join(", ", invalidDates)}";
            check.ValidationMessages.Add("Document contains dates outside expected range for financial aid purposes");
        }
        else
        {
            check.Passed = true;
            check.Confidence = 1.0m;
            check.Details = "All dates are within acceptable range";
        }
        return Task.CompletedTask;
    }

    private DocumentVerificationStatus DetermineVerificationStatus(VerificationResult result, VerificationType type)
    {
        // If any required checks failed, require manual review
        if (result.Issues.Any() || result.OverallScore < 0.8m)
        {
            return DocumentVerificationStatus.ManualReviewRequired;
        }

        // If confidence is very high and all checks passed, auto-approve
        if (result.OverallScore >= 0.95m && result.Checks.All(c => c.Passed))
        {
            return DocumentVerificationStatus.AutoApproved;
        }

        // Otherwise, require manual review for safety
        return DocumentVerificationStatus.ManualReviewRequired;
    }

    private async Task SaveVerificationResultAsync(VerificationResult result, string userId)
    {
        var entity = result.ToEntity(userId);
        
        // Remove any existing verification for this document
        var existing = await _dbContext.DocumentVerifications
            .Where(v => v.DocumentId == result.DocumentId)
            .ToListAsync();
            
        _dbContext.DocumentVerifications.RemoveRange(existing);
        _dbContext.DocumentVerifications.Add(entity);
        
        await _dbContext.SaveChangesAsync();
    }

    private async Task SendVerificationNotificationAsync(VerificationResult result, Guid userId)
    {
        try
        {
            switch (result.Status)
            {
                case DocumentVerificationStatus.AutoApproved:
                    await _notificationService.SendDocumentApprovedNotificationAsync(result.DocumentId, userId);
                    break;
                    
                case DocumentVerificationStatus.ManualReviewRequired:
                    await _notificationService.SendDocumentReviewRequiredNotificationAsync(result.DocumentId, userId);
                    break;
                    
                case DocumentVerificationStatus.Rejected:
                    await _notificationService.SendDocumentRejectedNotificationAsync(
                        result.DocumentId, userId, "Automatic verification failed", result.Issues);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification notification for document {DocumentId}", result.DocumentId);
        }
    }

    private Dictionary<DocumentType, List<VerificationRule>> InitializeVerificationRules()
    {
        var rules = new Dictionary<DocumentType, List<VerificationRule>>();

        // Tax Return Rules
        rules[DocumentType.TaxReturn] = new List<VerificationRule>
        {
            new()
            {
                RuleName = "Tax Return Content Validation",
                Description = "Validate extracted tax form data meets confidence requirements",
                ApplicableDocumentType = DocumentType.TaxReturn,
                CheckType = VerificationCheckType.ContentValidation,
                IsRequired = true,
                MinimumScore = 0.85m
            },
            new()
            {
                RuleName = "Tax Return Required Fields",
                Description = "Ensure all required tax fields are present",
                ApplicableDocumentType = DocumentType.TaxReturn,
                CheckType = VerificationCheckType.RequiredFieldCheck,
                IsRequired = true,
                Parameters = new Dictionary<string, object>
                {
                    ["requiredFields"] = new List<string> { "FirstName", "LastName", "SSN", "AGI", "FilingStatus" }
                }
            },
            new()
            {
                RuleName = "Tax Year Validation",
                Description = "Validate tax year is within acceptable range",
                ApplicableDocumentType = DocumentType.TaxReturn,
                CheckType = VerificationCheckType.DateRangeCheck,
                IsRequired = true
            }
        };

        // W-2 Form Rules
        rules[DocumentType.W2Form] = new List<VerificationRule>
        {
            new()
            {
                RuleName = "W-2 Content Validation",
                Description = "Validate extracted W-2 data meets confidence requirements",
                ApplicableDocumentType = DocumentType.W2Form,
                CheckType = VerificationCheckType.ContentValidation,
                IsRequired = true,
                MinimumScore = 0.80m
            },
            new()
            {
                RuleName = "W-2 Required Fields",
                Description = "Ensure all required W-2 fields are present",
                ApplicableDocumentType = DocumentType.W2Form,
                CheckType = VerificationCheckType.RequiredFieldCheck,
                IsRequired = true,
                Parameters = new Dictionary<string, object>
                {
                    ["requiredFields"] = new List<string> { "EmployeeName", "EmployeeSSN", "EmployerName", "WagesAmount" }
                }
            }
        };

        // Bank Statement Rules
        rules[DocumentType.BankStatement] = new List<VerificationRule>
        {
            new()
            {
                RuleName = "Bank Statement Format Check",
                Description = "Validate document format and basic properties",
                ApplicableDocumentType = DocumentType.BankStatement,
                CheckType = VerificationCheckType.FormatCheck,
                IsRequired = true
            },
            new()
            {
                RuleName = "Bank Statement Date Range",
                Description = "Validate statement dates are recent and reasonable",
                ApplicableDocumentType = DocumentType.BankStatement,
                CheckType = VerificationCheckType.DateRangeCheck,
                IsRequired = false
            }
        };

        // Default rules for other document types
        rules[DocumentType.Other] = new List<VerificationRule>
        {
            new()
            {
                RuleName = "Basic Format Check",
                Description = "Validate document format and basic properties",
                ApplicableDocumentType = DocumentType.Other,
                CheckType = VerificationCheckType.FormatCheck,
                IsRequired = true
            },
            new()
            {
                RuleName = "Basic Content Validation",
                Description = "Validate extracted data meets minimum confidence requirements",
                ApplicableDocumentType = DocumentType.Other,
                CheckType = VerificationCheckType.ContentValidation,
                IsRequired = false,
                MinimumScore = 0.70m
            }
        };

        return rules;
    }
}