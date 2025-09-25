namespace finaid.Configuration;

/// <summary>
/// Configuration settings for AWS Textract Service integration
/// </summary>
public class AWSTextractSettings
{
    public const string SectionName = "AWSTextract";
    
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
    /// Confidence threshold for extracted data (0.0 to 1.0)
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.7;
    
    /// <summary>
    /// Maximum processing time in seconds
    /// </summary>
    public int MaxProcessingTimeSeconds { get; set; } = 120;
    
    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Whether to enable logging
    /// </summary>
    public bool EnableLogging { get; set; } = true;
    
    /// <summary>
    /// Whether to log raw results (for debugging)
    /// </summary>
    public bool LogRawResults { get; set; } = false;
    
    /// <summary>
    /// Model mappings for different document types
    /// </summary>
    public Dictionary<string, string> ModelMappings { get; set; } = new()
    {
        { "TaxReturn", "TAX_FORM" },
        { "W2Form", "W2_FORM" },
        { "BankStatement", "DOCUMENT" },
        { "DriversLicense", "ID_DOCUMENT" },
        { "Passport", "ID_DOCUMENT" }
    };
}