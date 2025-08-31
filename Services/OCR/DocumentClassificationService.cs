using finaid.Models.Document;
using finaid.Models.OCR;
using finaid.Services.Storage;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace finaid.Services.OCR;

public class DocumentClassificationService
{
    private readonly IOCRService _ocrService;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly ILogger<DocumentClassificationService> _logger;
    
    // Classification rules based on content patterns
    private readonly Dictionary<DocumentType, List<ClassificationRule>> _classificationRules;

    public DocumentClassificationService(
        IOCRService ocrService,
        IDocumentStorageService documentStorageService,
        ILogger<DocumentClassificationService> logger)
    {
        _ocrService = ocrService;
        _documentStorageService = documentStorageService;
        _logger = logger;
        _classificationRules = InitializeClassificationRules();
    }

    public async Task<ClassificationResult> ClassifyDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting document classification for {DocumentId}", documentId);

            // First get the raw text using basic OCR
            var documentStream = await _documentStorageService.DownloadDocumentAsync(documentId, cancellationToken);
            var metadata = await _documentStorageService.GetDocumentMetadataAsync(documentId, cancellationToken);
            
            if (metadata == null)
            {
                throw new InvalidOperationException($"Document metadata not found for {documentId}");
            }

            // Use the OCR service to get text content
            var ocrResult = await _ocrService.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken);
            var content = ocrResult.RawText.ToLowerInvariant();
            
            // Apply classification rules
            var classifications = new List<(DocumentType Type, decimal Score)>();
            
            foreach (var (documentType, rules) in _classificationRules)
            {
                var score = CalculateTypeScore(content, rules);
                if (score > 0)
                {
                    classifications.Add((documentType, score));
                }
            }

            // Sort by score and get the best match
            var bestMatch = classifications
                .OrderByDescending(c => c.Score)
                .FirstOrDefault();

            var result = new ClassificationResult
            {
                DocumentId = documentId,
                ClassifiedType = bestMatch.Type != default ? bestMatch.Type : DocumentType.Other,
                Confidence = bestMatch.Score,
                ProcessedAt = DateTime.UtcNow,
                AllScores = classifications.ToDictionary(c => c.Type, c => c.Score)
            };

            // Add additional context based on filename
            result.Confidence = AdjustConfidenceBasedOnFilename(result.Confidence, metadata.FileName, result.ClassifiedType);

            _logger.LogInformation("Document {DocumentId} classified as {DocumentType} with confidence {Confidence}", 
                documentId, result.ClassifiedType, result.Confidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document classification failed for {DocumentId}", documentId);
            return new ClassificationResult
            {
                DocumentId = documentId,
                ClassifiedType = DocumentType.Other,
                Confidence = 0m,
                ProcessedAt = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    public async Task<List<DocumentType>> GetSuggestedTypesAsync(Guid documentId, int maxSuggestions = 3, CancellationToken cancellationToken = default)
    {
        var classification = await ClassifyDocumentAsync(documentId, cancellationToken);
        
        return classification.AllScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(maxSuggestions)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private Dictionary<DocumentType, List<ClassificationRule>> InitializeClassificationRules()
    {
        return new Dictionary<DocumentType, List<ClassificationRule>>
        {
            {
                DocumentType.TaxReturn,
                new List<ClassificationRule>
                {
                    new() { Keywords = new[] { "form 1040", "1040ez", "1040a" }, Weight = 0.9m },
                    new() { Keywords = new[] { "tax return", "irs", "internal revenue" }, Weight = 0.7m },
                    new() { Keywords = new[] { "adjusted gross income", "agi", "taxable income" }, Weight = 0.6m },
                    new() { Keywords = new[] { "filing status", "exemptions", "deductions" }, Weight = 0.5m }
                }
            },
            {
                DocumentType.W2Form,
                new List<ClassificationRule>
                {
                    new() { Keywords = new[] { "form w-2", "w2", "wage and tax statement" }, Weight = 0.95m },
                    new() { Keywords = new[] { "wages, tips", "federal income tax withheld" }, Weight = 0.8m },
                    new() { Keywords = new[] { "employer identification number", "ein" }, Weight = 0.7m },
                    new() { Keywords = new[] { "social security wages", "medicare wages" }, Weight = 0.6m }
                }
            },
            {
                DocumentType.BankStatement,
                new List<ClassificationRule>
                {
                    new() { Keywords = new[] { "bank statement", "account statement" }, Weight = 0.9m },
                    new() { Keywords = new[] { "beginning balance", "ending balance", "statement period" }, Weight = 0.8m },
                    new() { Keywords = new[] { "deposits", "withdrawals", "checks", "debit" }, Weight = 0.7m },
                    new() { Keywords = new[] { "account number", "routing number" }, Weight = 0.6m }
                }
            },
            {
                DocumentType.DriversLicense,
                new List<ClassificationRule>
                {
                    new() { Keywords = new[] { "driver license", "driver's license", "dl" }, Weight = 0.9m },
                    new() { Keywords = new[] { "license number", "expiration date", "date of birth" }, Weight = 0.8m },
                    new() { Keywords = new[] { "class", "restrictions", "endorsements" }, Weight = 0.6m },
                    new() { Keywords = new[] { "department of motor vehicles", "dmv" }, Weight = 0.5m }
                }
            },
            {
                DocumentType.Passport,
                new List<ClassificationRule>
                {
                    new() { Keywords = new[] { "passport", "united states of america" }, Weight = 0.95m },
                    new() { Keywords = new[] { "passport number", "nationality", "date of issue" }, Weight = 0.8m },
                    new() { Keywords = new[] { "place of birth", "date of expiration" }, Weight = 0.7m }
                }
            },
            {
                DocumentType.HighSchoolTranscript,
                new List<ClassificationRule>
                {
                    new() { Keywords = new[] { "high school", "transcript", "diploma" }, Weight = 0.9m },
                    new() { Keywords = new[] { "graduation date", "gpa", "credits" }, Weight = 0.8m },
                    new() { Keywords = new[] { "grade", "semester", "academic year" }, Weight = 0.6m }
                }
            },
            {
                DocumentType.CollegeTranscript,
                new List<ClassificationRule>
                {
                    new() { Keywords = new[] { "college", "university", "transcript" }, Weight = 0.9m },
                    new() { Keywords = new[] { "bachelor", "master", "degree", "cumulative gpa" }, Weight = 0.8m },
                    new() { Keywords = new[] { "credit hours", "semester", "course" }, Weight = 0.6m }
                }
            }
        };
    }

    private decimal CalculateTypeScore(string content, List<ClassificationRule> rules)
    {
        decimal totalScore = 0m;
        
        foreach (var rule in rules)
        {
            var keywordMatches = rule.Keywords.Count(keyword => content.Contains(keyword));
            var keywordScore = (decimal)keywordMatches / rule.Keywords.Length;
            totalScore += keywordScore * rule.Weight;

            // Apply regex patterns if available
            if (!string.IsNullOrEmpty(rule.Pattern))
            {
                try
                {
                    var regex = new Regex(rule.Pattern, RegexOptions.IgnoreCase);
                    if (regex.IsMatch(content))
                    {
                        totalScore += rule.PatternWeight;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", rule.Pattern);
                }
            }
        }

        // Normalize score to 0-1 range
        return Math.Min(totalScore, 1m);
    }

    private decimal AdjustConfidenceBasedOnFilename(decimal baseConfidence, string filename, DocumentType classifiedType)
    {
        var lowerFilename = filename.ToLowerInvariant();
        
        var filenameBoost = classifiedType switch
        {
            DocumentType.TaxReturn when lowerFilename.Contains("1040") || lowerFilename.Contains("tax") => 0.1m,
            DocumentType.W2Form when lowerFilename.Contains("w2") || lowerFilename.Contains("w-2") => 0.1m,
            DocumentType.BankStatement when lowerFilename.Contains("statement") || lowerFilename.Contains("bank") => 0.1m,
            DocumentType.DriversLicense when lowerFilename.Contains("license") || lowerFilename.Contains("dl") => 0.1m,
            DocumentType.Passport when lowerFilename.Contains("passport") => 0.1m,
            DocumentType.HighSchoolTranscript when lowerFilename.Contains("transcript") && lowerFilename.Contains("high") => 0.1m,
            DocumentType.CollegeTranscript when lowerFilename.Contains("transcript") && (lowerFilename.Contains("college") || lowerFilename.Contains("university")) => 0.1m,
            _ => 0m
        };

        return Math.Min(baseConfidence + filenameBoost, 1m);
    }
}

public class ClassificationResult
{
    public Guid DocumentId { get; set; }
    public DocumentType ClassifiedType { get; set; }
    public decimal Confidence { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Dictionary<DocumentType, decimal> AllScores { get; set; } = new();
    public string? Error { get; set; }
}

public class ClassificationRule
{
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public decimal Weight { get; set; } = 1.0m;
    public string? Pattern { get; set; }
    public decimal PatternWeight { get; set; } = 0.2m;
}