using finaid.Models.Documents;

namespace finaid.Services.Storage;

public interface IDocumentStorageService
{
    /// <summary>
    /// Upload a document to secure blob storage
    /// </summary>
    Task<StorageResult> UploadDocumentAsync(Stream documentStream, DocumentMetadata metadata, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Download a document stream from storage
    /// </summary>
    Task<Stream> DownloadDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a document from storage
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get document metadata without downloading the file
    /// </summary>
    Task<DocumentMetadata?> GetDocumentMetadataAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate a secure temporary download URL
    /// </summary>
    Task<string> GenerateDownloadUrlAsync(Guid documentId, TimeSpan expiry, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate file type and size before upload
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidateDocumentAsync(Stream documentStream, string fileName, long fileSizeBytes);
    
    /// <summary>
    /// Clean up expired documents
    /// </summary>
    Task<int> CleanupExpiredDocumentsAsync(CancellationToken cancellationToken = default);
}