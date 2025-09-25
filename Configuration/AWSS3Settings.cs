namespace finaid.Configuration;

/// <summary>
/// Configuration settings for AWS S3 Service integration
/// </summary>
public class AWSS3Settings
{
    public const string SectionName = "AWSS3";
    
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
    /// S3 Bucket name for document storage
    /// </summary>
    public string BucketName { get; set; } = "finaid-documents";
    
    /// <summary>
    /// Maximum file size in bytes
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 52428800; // 50MB
    
    /// <summary>
    /// Allowed file types
    /// </summary>
    public List<string> AllowedFileTypes { get; set; } = new() { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
    
    /// <summary>
    /// Whether to enable virus scanning
    /// </summary>
    public bool EnableVirusScanning { get; set; } = true;
    
    /// <summary>
    /// Whether to enable encryption
    /// </summary>
    public bool EnableEncryption { get; set; } = true;
    
    /// <summary>
    /// Presigned URL expiry in hours
    /// </summary>
    public int PresignedUrlExpiryHours { get; set; } = 24;
    
    /// <summary>
    /// Document retention in days
    /// </summary>
    public int DocumentRetentionDays { get; set; } = 2555; // ~7 years
    
    /// <summary>
    /// Whether to enable access logging
    /// </summary>
    public bool EnableAccessLogging { get; set; } = true;
}