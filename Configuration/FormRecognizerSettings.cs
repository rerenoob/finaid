namespace finaid.Configuration;

public class FormRecognizerSettings
{
    public const string SectionName = "FormRecognizer";
    
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    
    public float ConfidenceThreshold { get; set; } = 0.7f;
    public int MaxProcessingTimeSeconds { get; set; } = 120;
    public int MaxRetryAttempts { get; set; } = 3;
    
    public bool EnableCustomModels { get; set; } = false;
    public string CustomModelsContainer { get; set; } = string.Empty;
    
    public Dictionary<string, string> ModelMappings { get; set; } = new();
    
    public bool EnableLogging { get; set; } = true;
    public bool LogRawResults { get; set; } = false; // For debugging only
}