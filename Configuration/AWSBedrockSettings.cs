namespace finaid.Configuration;

/// <summary>
/// Configuration settings for AWS Bedrock Service integration
/// </summary>
public class AWSBedrockSettings
{
    public const string SectionName = "AWSBedrock";
    
    /// <summary>
    /// AWS Region (e.g., us-east-1, us-west-2)
    /// </summary>
    public string Region { get; set; } = "us-east-1";
    
    /// <summary>
    /// AWS Access Key ID
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// AWS Secret Access Key
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Model ID for the AI model (e.g., anthropic.claude-3-sonnet-20240229-v1:0)
    /// </summary>
    public string ModelId { get; set; } = "anthropic.claude-3-sonnet-20240229-v1:0";
    
    /// <summary>
    /// Maximum tokens allowed per request
    /// </summary>
    public int MaxTokens { get; set; } = 4000;
    
    /// <summary>
    /// Temperature for response creativity (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.3;
    
    /// <summary>
    /// Top-p sampling parameter
    /// </summary>
    public double TopP { get; set; } = 1.0;
    
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
    /// Whether to enable response streaming
    /// </summary>
    public bool EnableStreaming { get; set; } = true;
    
    /// <summary>
    /// Maximum conversation context to maintain
    /// </summary>
    public int MaxContextMessages { get; set; } = 20;
    
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
}