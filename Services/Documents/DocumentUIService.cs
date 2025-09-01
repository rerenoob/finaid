using finaid.Models.UI;
using finaid.Models.Documents;
using finaid.Models.Document;
using finaid.Services.Storage;
using finaid.Services.OCR;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Concurrent;

namespace finaid.Services.Documents;

public interface IDocumentUIService
{
    event Action<DocumentUploadState>? UploadStateChanged;
    event Action<List<DocumentMetadata>>? DocumentListChanged;
    
    Task<DocumentUploadState> StartUploadAsync(IBrowserFile file, Guid userId);
    Task CancelUploadAsync(Guid uploadId);
    Task RetryUploadAsync(Guid uploadId);
    DocumentUploadState? GetUploadState(Guid uploadId);
    List<DocumentUploadState> GetActiveUploads();
    
    Task<List<DocumentMetadata>> GetUserDocumentsAsync(Guid userId);
    Task<bool> DeleteDocumentAsync(Guid documentId);
    Task<string> GetDocumentDownloadUrlAsync(Guid documentId, TimeSpan expiry);
}

public class DocumentUIService : IDocumentUIService
{
    private readonly IDocumentStorageService _storageService;
    private readonly IOCRService _ocrService;
    private readonly DocumentClassificationService _classificationService;
    private readonly ILogger<DocumentUIService> _logger;
    
    private readonly ConcurrentDictionary<Guid, DocumentUploadState> _activeUploads = new();
    private readonly SemaphoreSlim _uploadSemaphore = new(3, 3); // Max 3 concurrent uploads

    public event Action<DocumentUploadState>? UploadStateChanged;
    public event Action<List<DocumentMetadata>>? DocumentListChanged;

    public DocumentUIService(
        IDocumentStorageService storageService,
        IOCRService ocrService,
        DocumentClassificationService classificationService,
        ILogger<DocumentUIService> logger)
    {
        _storageService = storageService;
        _ocrService = ocrService;
        _classificationService = classificationService;
        _logger = logger;
    }

    public async Task<DocumentUploadState> StartUploadAsync(IBrowserFile file, Guid userId)
    {
        var uploadState = new DocumentUploadState
        {
            FileName = file.Name,
            FileSize = file.Size,
            ContentType = file.ContentType,
            File = file,
            StartedAt = DateTime.UtcNow,
            CancellationTokenSource = new CancellationTokenSource()
        };

        _activeUploads[uploadState.Id] = uploadState;
        NotifyUploadStateChanged(uploadState);

        // Start upload process in background
        _ = Task.Run(() => ProcessUploadAsync(uploadState, userId));

        return uploadState;
    }

    public Task CancelUploadAsync(Guid uploadId)
    {
        if (_activeUploads.TryGetValue(uploadId, out var uploadState))
        {
            uploadState.Cancel();
            NotifyUploadStateChanged(uploadState);
        }
        
        return Task.CompletedTask;
    }

    public async Task RetryUploadAsync(Guid uploadId)
    {
        if (_activeUploads.TryGetValue(uploadId, out var uploadState) && 
            uploadState.Status == UploadStatus.Failed)
        {
            uploadState.Status = UploadStatus.Queued;
            uploadState.ErrorMessage = null;
            uploadState.BytesUploaded = 0;
            uploadState.StartedAt = DateTime.UtcNow;
            uploadState.CompletedAt = null;
            uploadState.CancellationTokenSource = new CancellationTokenSource();
            
            NotifyUploadStateChanged(uploadState);
            
            // Retry upload process
            _ = Task.Run(() => ProcessUploadAsync(uploadState, Guid.Empty)); // Need to get userId from context
        }
    }

    public DocumentUploadState? GetUploadState(Guid uploadId)
    {
        return _activeUploads.TryGetValue(uploadId, out var state) ? state : null;
    }

    public List<DocumentUploadState> GetActiveUploads()
    {
        return _activeUploads.Values
            .Where(u => u.Status != UploadStatus.Completed && 
                       u.Status != UploadStatus.Failed && 
                       u.Status != UploadStatus.Cancelled)
            .OrderBy(u => u.StartedAt)
            .ToList();
    }

    public Task<List<DocumentMetadata>> GetUserDocumentsAsync(Guid userId)
    {
        try
        {
            // This would typically come from a service that queries the database
            // For now, return empty list - to be implemented with proper data access
            return Task.FromResult(new List<DocumentMetadata>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user documents for {UserId}", userId);
            return Task.FromResult(new List<DocumentMetadata>());
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId)
    {
        try
        {
            var result = await _storageService.DeleteDocumentAsync(documentId);
            
            if (result)
            {
                // Notify document list changed
                NotifyDocumentListChanged(new List<DocumentMetadata>());
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<string> GetDocumentDownloadUrlAsync(Guid documentId, TimeSpan expiry)
    {
        try
        {
            return await _storageService.GenerateDownloadUrlAsync(documentId, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download URL for document {DocumentId}", documentId);
            throw;
        }
    }

    private async Task ProcessUploadAsync(DocumentUploadState uploadState, Guid userId)
    {
        await _uploadSemaphore.WaitAsync(uploadState.CancellationTokenSource!.Token);
        
        try
        {
            if (uploadState.CancellationTokenSource.Token.IsCancellationRequested)
                return;

            // Step 1: Validate file
            uploadState.Status = UploadStatus.Validating;
            NotifyUploadStateChanged(uploadState);

            var stream = uploadState.File!.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024); // 50MB limit
            var (isValid, errorMessage) = await _storageService.ValidateDocumentAsync(
                stream, uploadState.FileName, uploadState.FileSize);

            if (!isValid)
            {
                uploadState.Status = UploadStatus.Failed;
                uploadState.ErrorMessage = errorMessage;
                uploadState.CompletedAt = DateTime.UtcNow;
                NotifyUploadStateChanged(uploadState);
                return;
            }

            // Step 2: Create metadata and upload
            uploadState.Status = UploadStatus.Uploading;
            NotifyUploadStateChanged(uploadState);

            var metadata = new DocumentMetadata
            {
                Id = uploadState.Id,
                UserId = userId,
                FileName = uploadState.FileName,
                ContentType = uploadState.ContentType,
                FileSizeBytes = uploadState.FileSize,
                Type = uploadState.DetectedType ?? DocumentType.Other,
                UploadedAt = DateTime.UtcNow
            };

            // Reset stream position
            stream.Position = 0;
            
            // Upload with progress tracking
            var uploadResult = await UploadWithProgressAsync(stream, metadata, uploadState);

            if (!uploadResult.Success)
            {
                uploadState.Status = UploadStatus.Failed;
                uploadState.ErrorMessage = uploadResult.Error;
                uploadState.CompletedAt = DateTime.UtcNow;
                NotifyUploadStateChanged(uploadState);
                return;
            }

            // Step 3: Processing (OCR classification)
            uploadState.Status = UploadStatus.Processing;
            NotifyUploadStateChanged(uploadState);

            try
            {
                var classification = await _classificationService.ClassifyDocumentAsync(uploadState.Id);
                uploadState.DetectedType = classification.ClassifiedType;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Document classification failed for {UploadId}", uploadState.Id);
                // Don't fail the upload for classification errors
            }

            // Step 4: Complete
            uploadState.Status = UploadStatus.Completed;
            uploadState.BytesUploaded = uploadState.FileSize;
            uploadState.CompletedAt = DateTime.UtcNow;
            NotifyUploadStateChanged(uploadState);

            // Notify document list changed
            var documents = await GetUserDocumentsAsync(userId);
            NotifyDocumentListChanged(documents);

        }
        catch (OperationCanceledException)
        {
            uploadState.Status = UploadStatus.Cancelled;
            uploadState.CompletedAt = DateTime.UtcNow;
            NotifyUploadStateChanged(uploadState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {FileName}", uploadState.FileName);
            uploadState.Status = UploadStatus.Failed;
            uploadState.ErrorMessage = ex.Message;
            uploadState.CompletedAt = DateTime.UtcNow;
            NotifyUploadStateChanged(uploadState);
        }
        finally
        {
            _uploadSemaphore.Release();
        }
    }

    private async Task<StorageResult> UploadWithProgressAsync(
        Stream stream, 
        DocumentMetadata metadata, 
        DocumentUploadState uploadState)
    {
        // For now, just call the storage service directly
        // In a real implementation, you'd want to implement chunked upload with progress tracking
        var result = await _storageService.UploadDocumentAsync(stream, metadata, uploadState.CancellationTokenSource!.Token);
        
        if (result.Success)
        {
            uploadState.BytesUploaded = uploadState.FileSize;
        }
        
        return result;
    }

    private void NotifyUploadStateChanged(DocumentUploadState uploadState)
    {
        UploadStateChanged?.Invoke(uploadState);
    }

    private void NotifyDocumentListChanged(List<DocumentMetadata> documents)
    {
        DocumentListChanged?.Invoke(documents);
    }

    public void Dispose()
    {
        foreach (var upload in _activeUploads.Values)
        {
            upload.CancellationTokenSource?.Dispose();
        }
        _activeUploads.Clear();
        _uploadSemaphore.Dispose();
    }
}