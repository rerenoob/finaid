namespace finaid.Services.Security;

public interface IVirusScanningService
{
    /// <summary>
    /// Scan a document stream for viruses and malware
    /// </summary>
    Task<VirusScanResult> ScanDocumentAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the scanning service is available
    /// </summary>
    Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);
}

public class VirusScanResult
{
    public bool IsClean { get; set; }
    public string ScanStatus { get; set; } = string.Empty;
    public List<string> Threats { get; set; } = new();
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string? ScannerVersion { get; set; }
    public string? Error { get; set; }
    
    public static VirusScanResult CreateClean(string? scannerVersion = null)
    {
        return new VirusScanResult
        {
            IsClean = true,
            ScanStatus = "Clean",
            ScannerVersion = scannerVersion
        };
    }
    
    public static VirusScanResult CreateInfected(List<string> threats, string? scannerVersion = null)
    {
        return new VirusScanResult
        {
            IsClean = false,
            ScanStatus = "Infected",
            Threats = threats,
            ScannerVersion = scannerVersion
        };
    }
    
    public static VirusScanResult CreateError(string error)
    {
        return new VirusScanResult
        {
            IsClean = false,
            ScanStatus = "Error",
            Error = error
        };
    }
}