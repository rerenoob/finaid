using finaid.Models.AI;

namespace finaid.Services.Forms;

/// <summary>
/// Interface for AI-powered form assistance functionality
/// </summary>
public interface IFormAssistanceService
{
    /// <summary>
    /// Get contextual field suggestions based on current form state
    /// </summary>
    /// <param name="fieldName">Name of the form field</param>
    /// <param name="currentValue">Current field value</param>
    /// <param name="formContext">Complete form context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Field suggestion with help text and recommended actions</returns>
    Task<FormFieldSuggestion> GetFieldSuggestionAsync(string fieldName, object? currentValue, object formContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a form field and provide AI-powered feedback
    /// </summary>
    /// <param name="fieldName">Name of the form field</param>
    /// <param name="value">Field value to validate</param>
    /// <param name="formContext">Complete form context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of validation suggestions</returns>
    Task<List<ValidationSuggestion>> ValidateFieldAsync(string fieldName, object? value, object formContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explain what a specific form field requires
    /// </summary>
    /// <param name="fieldName">Name of the form field</param>
    /// <param name="formContext">Form context for personalized explanations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plain language explanation of field requirements</returns>
    Task<string> ExplainFieldRequirementAsync(string fieldName, object formContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggest a value for a form field based on context
    /// </summary>
    /// <param name="fieldName">Name of the form field</param>
    /// <param name="formContext">Form context to base suggestions on</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Suggested field value or null if no suggestion available</returns>
    Task<object?> SuggestFieldValueAsync(string fieldName, object formContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get AI-powered form completion assistance
    /// </summary>
    /// <param name="formType">Type of form being completed</param>
    /// <param name="currentProgress">Current completion progress (0.0 to 1.0)</param>
    /// <param name="formContext">Complete form state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Assistance recommendations and next steps</returns>
    Task<FormCompletionAssistance> GetCompletionAssistanceAsync(string formType, double currentProgress, object formContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate form summary and review suggestions
    /// </summary>
    /// <param name="formData">Complete form data</param>
    /// <param name="formType">Type of form</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Form review summary with suggestions</returns>
    Task<FormReviewSummary> GenerateFormReviewAsync(object formData, string formType, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI suggestion for a form field
/// </summary>
public class FormFieldSuggestion
{
    public string FieldName { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;
    public object? SuggestedValue { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
    public double Confidence { get; set; } = 1.0;
    public string? WarningMessage { get; set; }
    public bool RequiresUserAttention { get; set; } = false;
}

/// <summary>
/// Validation suggestion from AI analysis
/// </summary>
public class ValidationSuggestion
{
    public string FieldName { get; set; } = string.Empty;
    public string SuggestionType { get; set; } = string.Empty; // "error", "warning", "info", "improvement"
    public string Message { get; set; } = string.Empty;
    public string? SuggestedFix { get; set; }
    public object? SuggestedValue { get; set; }
    public string Severity { get; set; } = "medium"; // "low", "medium", "high", "critical"
    public bool IsBlocking { get; set; } = false;
}

/// <summary>
/// AI-powered form completion assistance
/// </summary>
public class FormCompletionAssistance
{
    public string FormType { get; set; } = string.Empty;
    public double CompletionPercentage { get; set; }
    public List<string> NextSteps { get; set; } = new();
    public List<string> MissingRequiredFields { get; set; } = new();
    public List<string> OptionalImprovements { get; set; } = new();
    public string? PriorityRecommendation { get; set; }
    public TimeSpan EstimatedTimeToComplete { get; set; }
    public List<string> RequiredDocuments { get; set; } = new();
}

/// <summary>
/// Form review summary with AI analysis
/// </summary>
public class FormReviewSummary
{
    public string FormType { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsReadyForSubmission { get; set; }
    public List<ValidationSuggestion> Issues { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> ImprovementSuggestions { get; set; } = new();
    public string? OverallAssessment { get; set; }
    public double QualityScore { get; set; } // 0.0 to 1.0
    public List<string> FinalChecklist { get; set; } = new();
}