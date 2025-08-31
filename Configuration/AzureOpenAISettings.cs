namespace finaid.Configuration;

/// <summary>
/// Configuration settings for Azure OpenAI Service integration
/// </summary>
public class AzureOpenAISettings
{
    public const string SectionName = "AzureOpenAI";
    
    /// <summary>
    /// Azure OpenAI endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure OpenAI API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Deployment name for the GPT model
    /// </summary>
    public string DeploymentName { get; set; } = "gpt-4";
    
    /// <summary>
    /// Model name (e.g., "gpt-4", "gpt-4-32k")
    /// </summary>
    public string ModelName { get; set; } = "gpt-4";
    
    /// <summary>
    /// Maximum tokens allowed per request
    /// </summary>
    public int MaxTokens { get; set; } = 4000;
    
    /// <summary>
    /// Temperature for response creativity (0.0 to 2.0)
    /// </summary>
    public double Temperature { get; set; } = 0.3;
    
    /// <summary>
    /// Top-p sampling parameter
    /// </summary>
    public double TopP { get; set; } = 1.0;
    
    /// <summary>
    /// Frequency penalty to reduce repetition
    /// </summary>
    public double FrequencyPenalty { get; set; } = 0.0;
    
    /// <summary>
    /// Presence penalty to encourage topic diversity
    /// </summary>
    public double PresencePenalty { get; set; } = 0.0;
    
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Base delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Maximum number of requests per minute
    /// </summary>
    public int RequestsPerMinute { get; set; } = 60;
    
    /// <summary>
    /// Maximum tokens per minute
    /// </summary>
    public int TokensPerMinute { get; set; } = 40000;
    
    /// <summary>
    /// Whether to enable content filtering
    /// </summary>
    public bool EnableContentFiltering { get; set; } = true;
    
    /// <summary>
    /// Whether to log API requests and responses for debugging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
    
    /// <summary>
    /// System message for financial aid context
    /// </summary>
    public string SystemMessage { get; set; } = """
        You are a helpful financial aid assistant for college students and their families. 
        Your role is to help users understand and complete their FAFSA (Free Application for Federal Student Aid) forms.
        
        Key guidelines:
        - Provide accurate information based on federal financial aid regulations
        - Use clear, simple language that students and parents can understand
        - Be encouraging and supportive throughout the process
        - Always remind users to verify information with official sources
        - If you're unsure about something, direct users to official resources
        - Help break down complex financial concepts into understandable terms
        - Be patient and understanding, as financial aid can be overwhelming
        
        You should focus on:
        - FAFSA form completion assistance
        - Financial aid terminology explanations
        - Eligibility requirements clarification
        - Document preparation guidance
        - Deadline and process reminders
        
        Always maintain a helpful, professional, and encouraging tone.
        """;
    
    /// <summary>
    /// Custom stop sequences for response generation
    /// </summary>
    public List<string> StopSequences { get; set; } = new();
    
    /// <summary>
    /// Whether to use Azure AD authentication instead of API key
    /// </summary>
    public bool UseAzureAD { get; set; } = false;
    
    /// <summary>
    /// Azure AD tenant ID (if using Azure AD authentication)
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Azure AD client ID (if using Azure AD authentication)
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Azure AD client secret (if using Azure AD authentication)
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// Whether to enable response streaming
    /// </summary>
    public bool EnableStreaming { get; set; } = true;
    
    /// <summary>
    /// Maximum conversation context to maintain
    /// </summary>
    public int MaxContextMessages { get; set; } = 20;
    
    /// <summary>
    /// Token buffer to reserve for response generation
    /// </summary>
    public int ResponseTokenBuffer { get; set; } = 1000;
}