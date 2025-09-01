using finaid.Services.AI;
using finaid.Services.Knowledge;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace finaid.Services.Forms;

/// <summary>
/// Implementation of AI-powered form assistance functionality
/// </summary>
public class FormAssistanceService : IFormAssistanceService
{
    private readonly IAIAssistantService _aiService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FormAssistanceService> _logger;
    
    private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromMinutes(15);

    public FormAssistanceService(
        IAIAssistantService aiService,
        IKnowledgeService knowledgeService,
        IMemoryCache cache,
        ILogger<FormAssistanceService> logger)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FormFieldSuggestion> GetFieldSuggestionAsync(string fieldName, object? currentValue, object formContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new FormFieldSuggestion
            {
                FieldName = fieldName ?? string.Empty,
                HelpText = "Field name is required",
                RequiresUserAttention = true,
                WarningMessage = "Invalid field name provided"
            };
        }

        try
        {
            var cacheKey = $"field_suggestion_{fieldName}_{GetCacheKeyFromContext(formContext)}";
            if (_cache.TryGetValue(cacheKey, out FormFieldSuggestion? cachedSuggestion))
            {
                _logger.LogDebug("Retrieved cached suggestion for field: {FieldName}", fieldName);
                return cachedSuggestion!;
            }

            // Get contextual knowledge about the field
            var fieldKnowledge = await _knowledgeService.GetPlainLanguageExplanationAsync(fieldName, "beginner", cancellationToken);
            
            var prompt = BuildFieldSuggestionPrompt(fieldName, currentValue, formContext, fieldKnowledge);
            var aiResponse = await _aiService.GetFormAssistanceAsync(prompt, formContext, cancellationToken);

            var suggestion = new FormFieldSuggestion
            {
                FieldName = fieldName,
                HelpText = aiResponse.IsSuccess ? aiResponse.Content : "Unable to provide guidance at this time",
                Confidence = aiResponse.IsSuccess ? 0.8 : 0.3
            };

            // Extract specific recommendations from AI response
            await EnhanceSuggestionWithAIAnalysis(suggestion, aiResponse.Content, fieldName);

            _cache.Set(cacheKey, suggestion, DefaultCacheExpiry);
            _logger.LogInformation("Generated field suggestion for: {FieldName}", fieldName);

            return suggestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating field suggestion for: {FieldName}", fieldName);
            return new FormFieldSuggestion
            {
                FieldName = fieldName,
                HelpText = "Temporary error retrieving field guidance. Please refer to the form instructions.",
                RequiresUserAttention = true,
                WarningMessage = "Service temporarily unavailable"
            };
        }
    }

    public async Task<List<ValidationSuggestion>> ValidateFieldAsync(string fieldName, object? value, object formContext, CancellationToken cancellationToken = default)
    {
        var suggestions = new List<ValidationSuggestion>();

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            suggestions.Add(new ValidationSuggestion
            {
                FieldName = fieldName ?? string.Empty,
                SuggestionType = "error",
                Message = "Field name is required for validation",
                Severity = "high",
                IsBlocking = true
            });
            return suggestions;
        }

        try
        {
            // Get field requirements from knowledge base
            var fieldTerm = await _knowledgeService.GetTermDefinitionAsync(fieldName, cancellationToken);
            var fieldRequirements = fieldTerm?.Definition;
            
            // Perform basic validation first
            var basicValidation = PerformBasicValidation(fieldName, value, fieldRequirements);
            suggestions.AddRange(basicValidation);

            // Use AI for advanced contextual validation
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                var aiValidation = await PerformAIValidation(fieldName, value, formContext, cancellationToken);
                suggestions.AddRange(aiValidation);
            }

            _logger.LogInformation("Validated field {FieldName} with {SuggestionCount} suggestions", fieldName, suggestions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating field: {FieldName}", fieldName);
            suggestions.Add(new ValidationSuggestion
            {
                FieldName = fieldName,
                SuggestionType = "warning",
                Message = "Unable to fully validate this field. Please review manually.",
                Severity = "medium"
            });
        }

        return suggestions;
    }

    public async Task<string> ExplainFieldRequirementAsync(string fieldName, object formContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return "Please specify a field name to explain.";
        }

        try
        {
            var cacheKey = $"field_explanation_{fieldName}";
            if (_cache.TryGetValue(cacheKey, out string? cachedExplanation))
            {
                _logger.LogDebug("Retrieved cached explanation for field: {FieldName}", fieldName);
                return cachedExplanation!;
            }

            // Get base explanation from knowledge service
            var baseExplanation = await _knowledgeService.GetPlainLanguageExplanationAsync(fieldName, "beginner", cancellationToken);
            
            // Enhance with AI for personalized context
            var enhancedExplanation = await EnhanceExplanationWithContext(fieldName, baseExplanation, formContext, cancellationToken);
            
            _cache.Set(cacheKey, enhancedExplanation, TimeSpan.FromHours(1));
            _logger.LogInformation("Generated explanation for field: {FieldName}", fieldName);

            return enhancedExplanation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining field requirement for: {FieldName}", fieldName);
            return $"Unable to explain the '{fieldName}' field requirements at this time. Please refer to the official form instructions or contact support.";
        }
    }

    public async Task<object?> SuggestFieldValueAsync(string fieldName, object formContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        try
        {
            // Get suggestions based on form context and user history
            var contextualSuggestion = await GetContextualSuggestion(fieldName, formContext, cancellationToken);
            
            _logger.LogInformation("Generated value suggestion for field: {FieldName}", fieldName);
            return contextualSuggestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting value for field: {FieldName}", fieldName);
            return null;
        }
    }

    public async Task<FormCompletionAssistance> GetCompletionAssistanceAsync(string formType, double currentProgress, object formContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var assistance = new FormCompletionAssistance
            {
                FormType = formType,
                CompletionPercentage = Math.Max(0, Math.Min(1, currentProgress))
            };

            // Analyze form context to determine next steps
            await AnalyzeFormCompletion(assistance, formContext, cancellationToken);
            
            _logger.LogInformation("Generated completion assistance for {FormType}, {Progress:P0} complete", 
                formType, currentProgress);

            return assistance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating completion assistance for {FormType}", formType);
            return new FormCompletionAssistance
            {
                FormType = formType,
                CompletionPercentage = currentProgress,
                NextSteps = { "Unable to analyze form completion. Please review the form manually." },
                PriorityRecommendation = "Review form sections and complete any missing required fields."
            };
        }
    }

    public async Task<FormReviewSummary> GenerateFormReviewAsync(object formData, string formType, CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = new FormReviewSummary
            {
                FormType = formType,
                IsComplete = false,
                IsReadyForSubmission = false
            };

            // Use AI to analyze the complete form
            await AnalyzeFormForReview(summary, formData, cancellationToken);
            
            _logger.LogInformation("Generated form review for {FormType}, Quality Score: {QualityScore:F2}", 
                formType, summary.QualityScore);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating form review for {FormType}", formType);
            return new FormReviewSummary
            {
                FormType = formType,
                IsComplete = false,
                IsReadyForSubmission = false,
                Issues = { 
                    new ValidationSuggestion
                    {
                        SuggestionType = "error",
                        Message = "Unable to analyze form. Please review manually before submission.",
                        Severity = "high"
                    }
                },
                OverallAssessment = "Form analysis unavailable. Manual review required."
            };
        }
    }

    private static string BuildFieldSuggestionPrompt(string fieldName, object? currentValue, object formContext, string fieldKnowledge)
    {
        var contextJson = JsonSerializer.Serialize(formContext, new JsonSerializerOptions { WriteIndented = true });
        var valueText = currentValue?.ToString() ?? "empty";

        return $"""
            I need help with the '{fieldName}' field on my FAFSA form.
            
            Current value: {valueText}
            Field guidance: {fieldKnowledge}
            
            Form context:
            {contextJson}
            
            Please provide:
            1. Clear explanation of what this field requires
            2. Specific guidance based on my current form state
            3. Any warnings or important considerations
            4. Suggested next steps
            """;
    }

    private Task EnhanceSuggestionWithAIAnalysis(FormFieldSuggestion suggestion, string aiContent, string fieldName)
    {
        // Parse AI response for actionable recommendations
        if (aiContent.Contains("required", StringComparison.OrdinalIgnoreCase) || 
            aiContent.Contains("must", StringComparison.OrdinalIgnoreCase))
        {
            suggestion.RequiresUserAttention = true;
        }

        if (aiContent.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
            aiContent.Contains("caution", StringComparison.OrdinalIgnoreCase))
        {
            suggestion.WarningMessage = ExtractWarningFromContent(aiContent);
        }

        // Extract actionable steps
        suggestion.RecommendedActions = ExtractRecommendedActions(aiContent);
        return Task.CompletedTask;
    }

    private List<ValidationSuggestion> PerformBasicValidation(string fieldName, object? value, string? requirements)
    {
        var suggestions = new List<ValidationSuggestion>();

        // Basic null/empty checks
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            if (requirements?.Contains("required", StringComparison.OrdinalIgnoreCase) == true)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    FieldName = fieldName,
                    SuggestionType = "error",
                    Message = "This field is required and cannot be left empty.",
                    Severity = "high",
                    IsBlocking = true
                });
            }
            return suggestions;
        }

        // Field-specific validations
        var stringValue = value.ToString()!;
        
        if (fieldName.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsValidEmail(stringValue))
            {
                suggestions.Add(new ValidationSuggestion
                {
                    FieldName = fieldName,
                    SuggestionType = "error",
                    Message = "Please enter a valid email address.",
                    Severity = "high",
                    IsBlocking = true
                });
            }
        }
        else if (fieldName.Contains("phone", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsValidPhone(stringValue))
            {
                suggestions.Add(new ValidationSuggestion
                {
                    FieldName = fieldName,
                    SuggestionType = "error",
                    Message = "Please enter a valid phone number.",
                    Severity = "medium",
                    SuggestedFix = "Format: (123) 456-7890 or 123-456-7890"
                });
            }
        }

        return suggestions;
    }

    private async Task<List<ValidationSuggestion>> PerformAIValidation(string fieldName, object value, object formContext, CancellationToken cancellationToken)
    {
        var suggestions = new List<ValidationSuggestion>();

        try
        {
            var prompt = $"""
                Please validate the '{fieldName}' field value: {value}
                
                Context: {JsonSerializer.Serialize(formContext)}
                
                Check for:
                - Data consistency with other fields
                - Common mistakes or issues
                - Optimization opportunities
                - Required documentation or follow-up
                
                Return suggestions as a JSON array of objects with: fieldName, suggestionType, message, severity, suggestedValue
                """;

            var aiResponse = await _aiService.GetFormAssistanceAsync(prompt, formContext, cancellationToken);
            
            if (aiResponse.IsSuccess)
            {
                // Parse AI response for validation suggestions
                // Note: This is simplified - in production, you'd want more robust JSON parsing
                ParseAIValidationResponse(suggestions, aiResponse.Content, fieldName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI validation failed for field: {FieldName}", fieldName);
        }

        return suggestions;
    }

    private async Task<string> EnhanceExplanationWithContext(string fieldName, string baseExplanation, object formContext, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"""
                Base explanation: {baseExplanation}
                
                User's form context: {JsonSerializer.Serialize(formContext)}
                
                Please provide a personalized explanation of the '{fieldName}' field that:
                1. Uses simple, clear language
                2. Relates to the user's specific situation based on their form data
                3. Includes a practical example if helpful
                4. Mentions any implications for financial aid eligibility
                """;

            var aiResponse = await _aiService.GetFormAssistanceAsync(prompt, formContext, cancellationToken);
            
            return aiResponse.IsSuccess ? aiResponse.Content : baseExplanation;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enhance explanation for field: {FieldName}", fieldName);
            return baseExplanation;
        }
    }

    private async Task<object?> GetContextualSuggestion(string fieldName, object formContext, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"""
                Based on the form context below, suggest an appropriate value for the '{fieldName}' field.
                
                Context: {JsonSerializer.Serialize(formContext)}
                
                Consider:
                - Previously entered values that might inform this field
                - Standard values for similar situations
                - Consistency with other form data
                
                Return only the suggested value, no additional text.
                """;

            var aiResponse = await _aiService.GetFormAssistanceAsync(prompt, formContext, cancellationToken);
            
            if (aiResponse.IsSuccess && !string.IsNullOrWhiteSpace(aiResponse.Content))
            {
                return aiResponse.Content.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get contextual suggestion for field: {FieldName}", fieldName);
        }

        return null;
    }

    private async Task AnalyzeFormCompletion(FormCompletionAssistance assistance, object formContext, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"""
                Analyze this FAFSA form completion status:
                {JsonSerializer.Serialize(formContext)}
                
                Current progress: {assistance.CompletionPercentage:P0}
                
                Provide:
                1. Missing required fields
                2. Recommended next steps (prioritized)
                3. Optional improvements
                4. Estimated time to complete
                5. Required documents still needed
                """;

            var aiResponse = await _aiService.GetFormAssistanceAsync(prompt, formContext, cancellationToken);
            
            if (aiResponse.IsSuccess)
            {
                ParseCompletionAnalysis(assistance, aiResponse.Content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze form completion");
            assistance.NextSteps.Add("Review form sections and complete any highlighted required fields.");
            assistance.EstimatedTimeToComplete = TimeSpan.FromMinutes(30);
        }
    }

    private async Task AnalyzeFormForReview(FormReviewSummary summary, object formData, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"""
                Review this completed FAFSA form for submission:
                {JsonSerializer.Serialize(formData)}
                
                Analyze:
                1. Completeness - are all required fields filled?
                2. Consistency - do the values make sense together?
                3. Common errors or issues
                4. Strengths of the application
                5. Final improvement suggestions
                6. Overall quality score (0-100)
                7. Submission readiness
                """;

            var aiResponse = await _aiService.GetFormAssistanceAsync(prompt, formData, cancellationToken);
            
            if (aiResponse.IsSuccess)
            {
                ParseFormReviewAnalysis(summary, aiResponse.Content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze form for review");
            summary.OverallAssessment = "Unable to complete automated review. Please review form manually.";
            summary.QualityScore = 0.5;
        }
    }

    // Helper methods
    private static string GetCacheKeyFromContext(object context)
    {
        try
        {
            var json = JsonSerializer.Serialize(context);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)))[..8];
        }
        catch
        {
            return "unknown";
        }
    }

    private static string ExtractWarningFromContent(string content)
    {
        // Simple extraction - in production, use more sophisticated parsing
        var lines = content.Split('\n');
        var warningLine = lines.FirstOrDefault(l => l.Contains("warning", StringComparison.OrdinalIgnoreCase));
        return warningLine?.Trim() ?? "Please review this field carefully.";
    }

    private static List<string> ExtractRecommendedActions(string content)
    {
        var actions = new List<string>();
        
        // Look for numbered or bulleted lists
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("1.") || trimmed.StartsWith("2.") || 
                trimmed.StartsWith("3.") || trimmed.StartsWith("4.") ||
                trimmed.StartsWith("- ") || trimmed.StartsWith("• "))
            {
                actions.Add(trimmed[2..].Trim());
            }
        }

        return actions.Take(5).ToList(); // Limit to 5 actions
    }

    private static void ParseAIValidationResponse(List<ValidationSuggestion> suggestions, string content, string fieldName)
    {
        // Simplified parsing - in production, implement robust JSON parsing with error handling
        if (content.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add(new ValidationSuggestion
            {
                FieldName = fieldName,
                SuggestionType = "error",
                Message = "AI validation identified potential issues with this field.",
                Severity = "medium"
            });
        }
    }

    private static void ParseCompletionAnalysis(FormCompletionAssistance assistance, string content)
    {
        // Simple parsing - extract key information from AI response
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var inNextSteps = false;
        var inMissingFields = false;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.Contains("next step", StringComparison.OrdinalIgnoreCase))
                inNextSteps = true;
            else if (trimmed.Contains("missing", StringComparison.OrdinalIgnoreCase))
                inMissingFields = true;
            else if (trimmed.StartsWith("- ") || trimmed.StartsWith("• "))
            {
                var item = trimmed[2..].Trim();
                if (inNextSteps)
                    assistance.NextSteps.Add(item);
                else if (inMissingFields)
                    assistance.MissingRequiredFields.Add(item);
            }
        }
        
        // Set defaults if parsing failed
        if (!assistance.NextSteps.Any())
        {
            assistance.NextSteps.Add("Complete any highlighted required fields");
            assistance.NextSteps.Add("Review form for accuracy");
        }
        
        assistance.EstimatedTimeToComplete = TimeSpan.FromMinutes(15 * assistance.MissingRequiredFields.Count);
    }

    private static void ParseFormReviewAnalysis(FormReviewSummary summary, string content)
    {
        // Simple parsing for form review
        summary.IsComplete = content.Contains("complete", StringComparison.OrdinalIgnoreCase) && 
                           !content.Contains("incomplete", StringComparison.OrdinalIgnoreCase);
        
        summary.IsReadyForSubmission = content.Contains("ready", StringComparison.OrdinalIgnoreCase) &&
                                     content.Contains("submission", StringComparison.OrdinalIgnoreCase);
        
        // Extract quality score if present
        var scoreMatch = System.Text.RegularExpressions.Regex.Match(content, @"(\d{1,3})(?:/100|%|\s*out\s*of\s*100)");
        if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out int score))
        {
            summary.QualityScore = Math.Max(0, Math.Min(1, score / 100.0));
        }
        else
        {
            summary.QualityScore = summary.IsComplete ? 0.8 : 0.5;
        }
        
        summary.OverallAssessment = content.Length > 100 ? content[..100] + "..." : content;
    }

    private static bool IsValidEmail(string email)
    {
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

    private static bool IsValidPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length == 10 || digits.Length == 11;
    }
}