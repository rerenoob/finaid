using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using finaid.Configuration;
using finaid.Models.Documents;
using finaid.Data;
using finaid.Data.Entities;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using DocumentStatus = finaid.Models.Document.DocumentStatus;

namespace finaid.Services.Storage;

public class AWSS3StorageService : IDocumentStorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly AWSS3Settings _settings;
    private readonly ILogger<AWSS3StorageService> _logger;
    private readonly ApplicationDbContext _dbContext;

    public AWSS3StorageService(
        AmazonS3Client s3Client,
        IOptions<AWSS3Settings> settings,
        ILogger<AWSS3StorageService> logger,
        ApplicationDbContext dbContext)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
        _dbContext = dbContext;
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

            // Generate unique S3 key
            var s3Key = GenerateS3Key(metadata.UserId, metadata.FileName);

            // Calculate file hash
            documentStream.Position = 0;
            var fileHash = await CalculateFileHashAsync(documentStream);
            documentStream.Position = 0;

            // Upload to S3
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = documentStream,
                Key = s3Key,
                BucketName = _settings.BucketName,
                ContentType = metadata.ContentType,
                AutoCloseStream = false
            };

            // Add metadata tags
            uploadRequest.Metadata.Add("UserId", metadata.UserId.ToString());
            uploadRequest.Metadata.Add("OriginalFileName", metadata.FileName);
            uploadRequest.Metadata.Add("DocumentType", metadata.Type.ToString());
            uploadRequest.Metadata.Add("UploadedAt", metadata.UploadedAt.ToString("O"));
            uploadRequest.Metadata.Add("FileHash", fileHash);
            uploadRequest.Metadata.Add("IsEncrypted", metadata.IsEncrypted.ToString());

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest, cancellationToken);

            // Save document record to database
            var userDocument = new UserDocument
            {
                Id = metadata.Id,
                UserId = metadata.UserId,
                DocumentType = metadata.Type,
                Status = DocumentStatus.Uploaded,
                FileName = metadata.FileName,
                BlobPath = s3Key,
                ContentType = metadata.ContentType,
                FileSizeBytes = metadata.FileSizeBytes,
                FileHash = fileHash,
                IsEncrypted = metadata.IsEncrypted,
                UploadedAt = metadata.UploadedAt,
                ExpiresAt = DateTime.UtcNow.AddDays(_settings.DocumentRetentionDays)
            };

            _dbContext.UserDocuments.Add(userDocument);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document uploaded successfully: {S3Key} for User: {UserId}", s3Key, metadata.UserId);

            return StorageResult.CreateSuccess(metadata.Id, s3Key, metadata.FileSizeBytes, fileHash);
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

            var request = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = metadata.BlobPath
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            
            _logger.LogInformation("Document downloaded: {DocumentId} by key: {S3Key}", documentId, metadata.BlobPath);
            
            return response.ResponseStream;
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

            var request = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = metadata.BlobPath
            };

            var response = await _s3Client.DeleteObjectAsync(request, cancellationToken);
            
            _logger.LogInformation("Document deleted: {DocumentId}, S3 Key: {S3Key}", documentId, metadata.BlobPath);
            
            return true;
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

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = metadata.BlobPath,
                Expires = DateTime.UtcNow.Add(expiry),
                Verb = HttpVerb.GET
            };

            var presignedUrl = _s3Client.GetPreSignedURL(request);
            
            _logger.LogInformation("Generated presigned URL for document: {DocumentId}, Expires: {Expires}", 
                documentId, request.Expires);
            
            return presignedUrl;
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
            
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _settings.BucketName
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);
                
                foreach (var s3Object in listResponse.S3Objects)
                {
                    if (s3Object.LastModified < cutoffDate)
                    {
                        var deleteRequest = new DeleteObjectRequest
                        {
                            BucketName = _settings.BucketName,
                            Key = s3Object.Key
                        };

                        await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
                        cleanedCount++;
                        _logger.LogInformation("Cleaned up expired document: {S3Key}", s3Object.Key);
                    }
                }
                
                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            } while (listResponse.IsTruncated);
            
            _logger.LogInformation("Cleanup completed. Removed {Count} expired documents", cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during document cleanup");
        }
        
        return cleanedCount;
    }

    private string GenerateS3Key(Guid userId, string fileName)
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