using System.ComponentModel.DataAnnotations;

namespace finaid.Models.AI;

/// <summary>
/// Wrapper for AI service responses with metadata and error handling
/// </summary>
public class AIResponse
{
    /// <summary>
    /// Whether the AI request was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// The AI-generated content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Specific error code for programmatic handling
    /// </summary>
    public AIErrorCode? ErrorCode { get; set; }
    
    /// <summary>
    /// Response metadata
    /// </summary>
    public AIResponseMetadata Metadata { get; set; } = new();
    
    /// <summary>
    /// Suggested follow-up actions for the user
    /// </summary>
    public List<string> SuggestedActions { get; set; } = new();
    
    /// <summary>
    /// Whether the response was retrieved from cache
    /// </summary>
    public bool FromCache { get; set; } = false;
    
    /// <summary>
    /// Confidence level of the AI response (0.0 to 1.0)
    /// </summary>
    public double ConfidenceLevel { get; set; } = 1.0;
    
    /// <summary>
    /// Whether the response requires additional user input
    /// </summary>
    public bool RequiresUserInput { get; set; } = false;
    
    /// <summary>
    /// Context that should be maintained for follow-up requests
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
    
    /// <summary>
    /// Create a successful response
    /// </summary>
    public static AIResponse Success(string content, AIResponseMetadata? metadata = null)
    {
        return new AIResponse
        {
            IsSuccess = true,
            Content = content,
            Metadata = metadata ?? new AIResponseMetadata()
        };
    }
    
    /// <summary>
    /// Create an error response
    /// </summary>
    public static AIResponse Error(string errorMessage, AIErrorCode? errorCode = null)
    {
        return new AIResponse
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Content = string.Empty
        };
    }
}

/// <summary>
/// Metadata about the AI response
/// </summary>
public class AIResponseMetadata
{
    /// <summary>
    /// Number of tokens used in the prompt
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Number of tokens in the response
    /// </summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// Total tokens used (prompt + completion)
    /// </summary>
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// Model used for the response
    /// </summary>
    public string? Model { get; set; }
    
    /// <summary>
    /// Time taken to process the request
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
    
    /// <summary>
    /// Finish reason from the model
    /// </summary>
    public string? FinishReason { get; set; }
    
    /// <summary>
    /// Content filter results
    /// </summary>
    public ContentFilterResults? ContentFilter { get; set; }
    
    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime ResponseTimestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Unique request ID for tracking
    /// </summary>
    public string? RequestId { get; set; }
}

/// <summary>
/// Content filter results from Azure OpenAI
/// </summary>
public class ContentFilterResults
{
    /// <summary>
    /// Hate content filter result
    /// </summary>
    public ContentFilterResult? Hate { get; set; }
    
    /// <summary>
    /// Self-harm content filter result
    /// </summary>
    public ContentFilterResult? SelfHarm { get; set; }
    
    /// <summary>
    /// Sexual content filter result
    /// </summary>
    public ContentFilterResult? Sexual { get; set; }
    
    /// <summary>
    /// Violence content filter result
    /// </summary>
    public ContentFilterResult? Violence { get; set; }
    
    /// <summary>
    /// Whether any filter was triggered
    /// </summary>
    public bool Filtered { get; set; }
}

/// <summary>
/// Individual content filter result
/// </summary>
public class ContentFilterResult
{
    /// <summary>
    /// Whether content was filtered
    /// </summary>
    public bool Filtered { get; set; }
    
    /// <summary>
    /// Severity level (safe, low, medium, high)
    /// </summary>
    public string? Severity { get; set; }
}

/// <summary>
/// AI error codes for specific error handling
/// </summary>
public enum AIErrorCode
{
    /// <summary>
    /// Unknown or unspecified error
    /// </summary>
    Unknown,
    
    /// <summary>
    /// API key is invalid or missing
    /// </summary>
    InvalidApiKey,
    
    /// <summary>
    /// Request exceeded rate limits
    /// </summary>
    RateLimitExceeded,
    
    /// <summary>
    /// Request exceeded token limits
    /// </summary>
    TokenLimitExceeded,
    
    /// <summary>
    /// Content was filtered by safety systems
    /// </summary>
    ContentFiltered,
    
    /// <summary>
    /// Request timeout
    /// </summary>
    Timeout,
    
    /// <summary>
    /// Service temporarily unavailable
    /// </summary>
    ServiceUnavailable,
    
    /// <summary>
    /// Model deployment not found
    /// </summary>
    ModelNotFound,
    
    /// <summary>
    /// Invalid request parameters
    /// </summary>
    InvalidRequest,
    
    /// <summary>
    /// Insufficient quota/credits
    /// </summary>
    InsufficientQuota,
    
    /// <summary>
    /// Network connectivity issues
    /// </summary>
    NetworkError
}