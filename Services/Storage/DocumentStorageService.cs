using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using finaid.Configuration;
using finaid.Models.Documents;
using finaid.Data;
using finaid.Data.Entities;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace finaid.Services.Storage;

public class DocumentStorageService : IDocumentStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly DocumentStorageSettings _settings;
    private readonly ILogger<DocumentStorageService> _logger;
    private readonly ApplicationDbContext _dbContext;

    public DocumentStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<DocumentStorageSettings> settings,
        ILogger<DocumentStorageService> logger,
        ApplicationDbContext dbContext)
    {
        _blobServiceClient = blobServiceClient;
        _settings = settings.Value;
        _logger = logger;
        _dbContext = dbContext;
        _containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
    }

    public async Task<StorageResult> UploadDocumentAsync(Stream documentStream, DocumentMetadata metadata, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the document
            var (isValid, errorMessage) = await ValidateDocumentAsync(documentStream, metadata.FileName, metadata.FileSizeBytes);
            if (!isValid)
            {
                return StorageResult.CreateFailure(errorMessage!);
            }

            // Generate unique blob path
            var blobPath = GenerateBlobPath(metadata.UserId, metadata.FileName);
            var blobClient = _containerClient.GetBlobClient(blobPath);

            // Calculate file hash
            documentStream.Position = 0;
            var fileHash = await CalculateFileHashAsync(documentStream);
            documentStream.Position = 0;

            // Set blob metadata
            var blobMetadata = new Dictionary<string, string>
            {
                ["UserId"] = metadata.UserId.ToString(),
                ["OriginalFileName"] = metadata.FileName,
                ["ContentType"] = metadata.ContentType,
                ["DocumentType"] = metadata.Type.ToString(),
                ["UploadedAt"] = metadata.UploadedAt.ToString("O"),
                ["FileHash"] = fileHash,
                ["IsEncrypted"] = metadata.IsEncrypted.ToString()
            };

            // Upload options
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = metadata.ContentType
                },
                Metadata = blobMetadata,
                Tags = new Dictionary<string, string>
                {
                    ["UserId"] = metadata.UserId.ToString(),
                    ["DocumentType"] = metadata.Type.ToString(),
                    ["UploadDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
                }
            };

            // Upload the blob
            var response = await blobClient.UploadAsync(documentStream, uploadOptions, cancellationToken);

            // Save document record to database
            var userDocument = new UserDocument
            {
                Id = metadata.Id,
                UserId = metadata.UserId,
                DocumentType = metadata.Type,
                Status = DocumentStatus.Uploaded,
                FileName = metadata.FileName,
                BlobPath = blobPath,
                ContentType = metadata.ContentType,
                FileSizeBytes = metadata.FileSizeBytes,
                FileHash = fileHash,
                IsEncrypted = metadata.IsEncrypted,
                UploadedAt = metadata.UploadedAt,
                ExpiresAt = DateTime.UtcNow.AddDays(_settings.DocumentRetentionDays)
            };

            _dbContext.UserDocuments.Add(userDocument);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document uploaded successfully: {BlobPath} for User: {UserId}", blobPath, metadata.UserId);

            return StorageResult.CreateSuccess(metadata.Id, blobPath, metadata.FileSizeBytes, fileHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document for User: {UserId}", metadata.UserId);
            return StorageResult.CreateFailure($"Upload failed: {ex.Message}");
        }
    }

    public async Task<Stream> DownloadDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await GetDocumentMetadataAsync(documentId, cancellationToken);
            if (metadata?.BlobPath == null)
            {
                throw new FileNotFoundException($"Document {documentId} not found");
            }

            var blobClient = _containerClient.GetBlobClient(metadata.BlobPath);
            
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Blob {metadata.BlobPath} not found in storage");
            }

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Document downloaded: {DocumentId} by path: {BlobPath}", documentId, metadata.BlobPath);
            
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download document: {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await GetDocumentMetadataAsync(documentId, cancellationToken);
            if (metadata?.BlobPath == null)
            {
                _logger.LogWarning("Attempted to delete non-existent document: {DocumentId}", documentId);
                return false;
            }

            var blobClient = _containerClient.GetBlobClient(metadata.BlobPath);
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            if (response.Value)
            {
                _logger.LogInformation("Document deleted: {DocumentId}, Blob: {BlobPath}", documentId, metadata.BlobPath);
            }
            
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document: {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userDocument = await _dbContext.UserDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            return userDocument?.ToMetadata();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document metadata: {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<string> GenerateDownloadUrlAsync(Guid documentId, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await GetDocumentMetadataAsync(documentId, cancellationToken);
            if (metadata?.BlobPath == null)
            {
                throw new FileNotFoundException($"Document {documentId} not found");
            }

            var blobClient = _containerClient.GetBlobClient(metadata.BlobPath);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Cannot generate SAS token with current authentication method");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = metadata.BlobPath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = blobClient.GenerateSasUri(sasBuilder);
            
            _logger.LogInformation("Generated SAS token for document: {DocumentId}, Expires: {ExpiresOn}", 
                documentId, sasBuilder.ExpiresOn);
            
            return sasToken.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download URL for document: {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateDocumentAsync(Stream documentStream, string fileName, long fileSizeBytes)
    {
        // Check file size
        if (fileSizeBytes > _settings.MaxFileSizeBytes)
        {
            return (false, $"File size {fileSizeBytes:N0} bytes exceeds maximum allowed size of {_settings.MaxFileSizeBytes:N0} bytes");
        }

        // Check file extension
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_settings.AllowedFileTypes.Contains(fileExtension))
        {
            return (false, $"File type '{fileExtension}' is not allowed. Supported types: {string.Join(", ", _settings.AllowedFileTypes)}");
        }

        // Basic file header validation
        documentStream.Position = 0;
        var buffer = new byte[512];
        var bytesRead = await documentStream.ReadAsync(buffer, 0, buffer.Length);
        
        if (!IsValidFileHeader(buffer, bytesRead, fileExtension))
        {
            return (false, "File header does not match the file extension");
        }

        return (true, null);
    }

    public async Task<int> CleanupExpiredDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var cleanedCount = 0;
        
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_settings.DocumentRetentionDays);
            
            await foreach (var blobItem in _containerClient.GetBlobsAsync(
                BlobTraits.Metadata | BlobTraits.Tags, 
                cancellationToken: cancellationToken))
            {
                if (blobItem.Properties.LastModified?.DateTime < cutoffDate)
                {
                    var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                    var deleted = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                    
                    if (deleted.Value)
                    {
                        cleanedCount++;
                        _logger.LogInformation("Cleaned up expired document: {BlobName}", blobItem.Name);
                    }
                }
            }
            
            _logger.LogInformation("Cleanup completed. Removed {Count} expired documents", cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during document cleanup");
        }
        
        return cleanedCount;
    }

    private string GenerateBlobPath(Guid userId, string fileName)
    {
        var sanitizedFileName = Path.GetFileName(fileName);
        var fileExtension = Path.GetExtension(sanitizedFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        
        return $"users/{userId}/{timestamp}-{uniqueId}-{nameWithoutExtension}{fileExtension}";
    }

    private async Task<string> CalculateFileHashAsync(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToBase64String(hash);
    }

    private bool IsValidFileHeader(byte[] buffer, int bytesRead, string fileExtension)
    {
        if (bytesRead < 4) return false;

        return fileExtension switch
        {
            ".pdf" => buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46, // %PDF
            ".jpg" or ".jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".png" => buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47,
            ".tiff" or ".tif" => (buffer[0] == 0x49 && buffer[1] == 0x49) || (buffer[0] == 0x4D && buffer[1] == 0x4D),
            _ => true // For now, allow unknown types to pass basic validation
        };
    }
}