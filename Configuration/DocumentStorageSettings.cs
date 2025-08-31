namespace finaid.Configuration;

public class DocumentStorageSettings
{
    public const string SectionName = "DocumentStorage";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB
    public string[] AllowedFileTypes { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
    public bool EnableVirusScanning { get; set; } = true;
    public bool EnableEncryption { get; set; } = true;
    public int SasTokenExpiryHours { get; set; } = 24;
    public int DocumentRetentionDays { get; set; } = 2555; // ~7 years
    public bool EnableAccessLogging { get; set; } = true;
}