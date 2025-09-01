using finaid.Models.Document;
using Microsoft.AspNetCore.Components.Forms;

namespace finaid.Models.UI;

public class DocumentUploadState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DocumentType? DetectedType { get; set; }
    public UploadStatus Status { get; set; } = UploadStatus.Queued;
    public long BytesUploaded { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public IBrowserFile? File { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }
    
    public decimal ProgressPercentage => FileSize > 0 ? (decimal)(BytesUploaded * 100) / FileSize : 0;
    
    public TimeSpan? ElapsedTime => Status switch
    {
        UploadStatus.Completed => CompletedAt?.Subtract(StartedAt),
        UploadStatus.Failed => CompletedAt?.Subtract(StartedAt),
        UploadStatus.Cancelled => CompletedAt?.Subtract(StartedAt),
        _ => DateTime.UtcNow.Subtract(StartedAt)
    };
    
    public string FormattedFileSize => FormatBytes(FileSize);
    public string FormattedUploadedSize => FormatBytes(BytesUploaded);
    
    private static string FormatBytes(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "B", "KB", "MB", "GB" };
        long max = (long)Math.Pow(scale, orders.Length - 1);
        
        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            max /= scale;
        }
        
        return $"{bytes} B";
    }
    
    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
        Status = UploadStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }
}

public enum UploadStatus
{
    Queued = 0,
    Validating = 1,
    Uploading = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}