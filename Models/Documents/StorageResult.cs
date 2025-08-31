namespace finaid.Models.Documents;

public class StorageResult
{
    public bool Success { get; set; }
    public Guid? DocumentId { get; set; }
    public string? BlobPath { get; set; }
    public string? Error { get; set; }
    public List<string> Warnings { get; set; } = new();
    public long? FileSizeBytes { get; set; }
    public string? FileHash { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public static StorageResult CreateFailure(string error)
    {
        return new StorageResult
        {
            Success = false,
            Error = error
        };
    }

    public static StorageResult CreateSuccess(Guid documentId, string blobPath, long fileSizeBytes, string fileHash)
    {
        return new StorageResult
        {
            Success = true,
            DocumentId = documentId,
            BlobPath = blobPath,
            FileSizeBytes = fileSizeBytes,
            FileHash = fileHash
        };
    }
}