using finaid.Data;
using finaid.Data.Entities;
using finaid.Models.Document;
using finaid.Models.OCR;
using finaid.Services.OCR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace finaid.BackgroundServices;

public class OCRProcessingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OCRProcessingService> _logger;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private const int MaxConcurrentProcessing = 5;
    private const int ProcessingIntervalSeconds = 30;

    public OCRProcessingService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<OCRProcessingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(MaxConcurrentProcessing, MaxConcurrentProcessing);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OCR Processing Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDocumentsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OCR Processing Service is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in OCR processing service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before retrying
            }
        }
    }

    private async Task ProcessPendingDocumentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Get documents that need OCR processing
        var pendingDocuments = await dbContext.UserDocuments
            .Where(d => d.Status == DocumentStatus.Uploaded)
            .Where(d => d.OCRJobId == null) // Not yet processed
            .OrderBy(d => d.UploadedAt)
            .Take(MaxConcurrentProcessing * 2) // Get a few extra to keep the pipeline full
            .ToListAsync(cancellationToken);

        if (!pendingDocuments.Any())
        {
            return; // No documents to process
        }

        _logger.LogInformation("Found {Count} documents pending OCR processing", pendingDocuments.Count);

        var processingTasks = pendingDocuments.Select(doc => 
            ProcessDocumentAsync(doc, cancellationToken)).ToList();

        await Task.WhenAll(processingTasks);
    }

    private async Task ProcessDocumentAsync(finaid.Data.Entities.UserDocument document, CancellationToken cancellationToken)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);
        
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ocrService = scope.ServiceProvider.GetRequiredService<IOCRService>();
            var classificationService = scope.ServiceProvider.GetRequiredService<DocumentClassificationService>();

            _logger.LogInformation("Starting OCR processing for document {DocumentId}", document.Id);

            // Mark document as processing
            document.Status = DocumentStatus.Processing;
            document.OCRJobId = Guid.NewGuid().ToString();
            document.ProcessingStartedAt = DateTime.UtcNow;
            
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                // Step 1: Classify the document if type is not already determined
                DocumentType documentType = document.DocumentType;
                if (documentType == DocumentType.Other)
                {
                    var classification = await classificationService.ClassifyDocumentAsync(document.Id, cancellationToken);
                    documentType = classification.ClassifiedType;
                    document.DocumentType = documentType;
                    document.ClassificationConfidence = classification.Confidence;
                }

                // Step 2: Perform OCR processing
                var ocrResult = await ocrService.ProcessDocumentAsync(document.Id, documentType, cancellationToken);

                // Step 3: Store OCR results
                await StoreOCRResultsAsync(document, ocrResult, dbContext, cancellationToken);

                // Step 4: Update document status based on processing results
                await UpdateDocumentStatusAsync(document, ocrResult, dbContext, cancellationToken);

                _logger.LogInformation("OCR processing completed successfully for document {DocumentId} with confidence {Confidence}", 
                    document.Id, ocrResult.OverallConfidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR processing failed for document {DocumentId}", document.Id);
                
                // Mark as failed and increment retry count
                document.Status = DocumentStatus.Rejected;
                document.ProcessingError = ex.Message;
                document.ProcessingCompletedAt = DateTime.UtcNow;
                document.RetryCount++;

                // Allow retry if under the limit
                if (document.RetryCount < 3)
                {
                    document.Status = DocumentStatus.Uploaded;
                    document.OCRJobId = null; // Clear job ID to allow reprocessing
                    document.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, document.RetryCount)); // Exponential backoff
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing document {DocumentId}", document.Id);
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private async Task StoreOCRResultsAsync(finaid.Data.Entities.UserDocument document, OCRResult ocrResult, ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        // Store OCR result in the database
        var ocrRecord = new OCRProcessingResult
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            ProcessedAt = ocrResult.ProcessedAt,
            OverallConfidence = ocrResult.OverallConfidence,
            RawText = ocrResult.RawText,
            ExtractedFields = JsonSerializer.Serialize(ocrResult.Fields),
            ValidationErrors = JsonSerializer.Serialize(ocrResult.ValidationErrors),
            ProcessingStatus = ocrResult.Status,
            ClassifiedType = ocrResult.ClassifiedType
        };

        dbContext.OCRProcessingResults.Add(ocrRecord);
        
        // Update the main document record with OCR reference
        document.OCRResultId = ocrRecord.Id;
        document.ExtractedText = ocrResult.RawText;
        document.OCRConfidence = ocrResult.OverallConfidence;
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateDocumentStatusAsync(finaid.Data.Entities.UserDocument document, OCRResult ocrResult, ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        document.ProcessingCompletedAt = DateTime.UtcNow;

        // Determine final status based on OCR results
        if (ocrResult.Status == ProcessingStatus.Failed)
        {
            document.Status = DocumentStatus.Rejected;
        }
        else if (ocrResult.Status == ProcessingStatus.RequiresReview || ocrResult.OverallConfidence < 0.8m)
        {
            document.Status = DocumentStatus.RequiresAction;
        }
        else if (ocrResult.ValidationErrors.Any())
        {
            document.Status = DocumentStatus.RequiresAction;
        }
        else
        {
            document.Status = DocumentStatus.Verified;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} status updated to {Status}", document.Id, document.Status);
    }

    public override void Dispose()
    {
        _concurrencySemaphore?.Dispose();
        base.Dispose();
    }
}