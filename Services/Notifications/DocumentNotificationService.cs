using finaid.Services.Storage;
using Microsoft.Extensions.Logging;

namespace finaid.Services.Notifications;

public class DocumentNotificationService : IDocumentNotificationService
{
    private readonly IDocumentStorageService _documentStorageService;
    private readonly ILogger<DocumentNotificationService> _logger;

    public DocumentNotificationService(
        IDocumentStorageService documentStorageService,
        ILogger<DocumentNotificationService> logger)
    {
        _documentStorageService = documentStorageService;
        _logger = logger;
    }

    public async Task SendDocumentApprovedNotificationAsync(Guid documentId, Guid userId)
    {
        try
        {
            var metadata = await _documentStorageService.GetDocumentMetadataAsync(documentId);
            if (metadata == null)
            {
                _logger.LogWarning("Cannot send approval notification: Document {DocumentId} metadata not found", documentId);
                return;
            }

            _logger.LogInformation("Document {DocumentId} ({FileName}) has been approved for user {UserId}", 
                documentId, metadata.FileName, userId);

            // In a real implementation, this would send email/SMS notifications
            // For now, we'll just log the notification
            await LogNotificationAsync(userId, "Document Approved", 
                $"Your document '{metadata.FileName}' has been approved and verified.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send document approved notification for {DocumentId}", documentId);
        }
    }

    public async Task SendDocumentRejectedNotificationAsync(Guid documentId, Guid userId, string reason, List<string> corrections)
    {
        try
        {
            var metadata = await _documentStorageService.GetDocumentMetadataAsync(documentId);
            if (metadata == null)
            {
                _logger.LogWarning("Cannot send rejection notification: Document {DocumentId} metadata not found", documentId);
                return;
            }

            var correctionsList = corrections.Any() 
                ? $"\n\nRequired corrections:\n• {string.Join("\n• ", corrections)}"
                : "";

            var message = $"Your document '{metadata.FileName}' requires attention.\n\nReason: {reason}{correctionsList}";

            _logger.LogInformation("Document {DocumentId} ({FileName}) has been rejected for user {UserId}: {Reason}", 
                documentId, metadata.FileName, userId, reason);

            await LogNotificationAsync(userId, "Document Requires Attention", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send document rejected notification for {DocumentId}", documentId);
        }
    }

    public async Task SendDocumentReviewRequiredNotificationAsync(Guid documentId, Guid userId)
    {
        try
        {
            var metadata = await _documentStorageService.GetDocumentMetadataAsync(documentId);
            if (metadata == null)
            {
                _logger.LogWarning("Cannot send review required notification: Document {DocumentId} metadata not found", documentId);
                return;
            }

            var message = $"Your document '{metadata.FileName}' is being reviewed by our verification team. " +
                         "You will receive an update once the review is complete.";

            _logger.LogInformation("Document {DocumentId} ({FileName}) requires manual review for user {UserId}", 
                documentId, metadata.FileName, userId);

            await LogNotificationAsync(userId, "Document Under Review", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send document review required notification for {DocumentId}", documentId);
        }
    }

    public async Task SendDocumentProcessedNotificationAsync(Guid documentId, Guid userId, bool wasSuccessful)
    {
        try
        {
            var metadata = await _documentStorageService.GetDocumentMetadataAsync(documentId);
            if (metadata == null)
            {
                _logger.LogWarning("Cannot send processing notification: Document {DocumentId} metadata not found", documentId);
                return;
            }

            var status = wasSuccessful ? "successfully processed" : "failed to process";
            var message = $"Your document '{metadata.FileName}' has been {status}.";

            if (!wasSuccessful)
            {
                message += " Please try uploading again or contact support if the problem persists.";
            }

            _logger.LogInformation("Document {DocumentId} ({FileName}) processing {Status} for user {UserId}", 
                documentId, metadata.FileName, wasSuccessful ? "succeeded" : "failed", userId);

            await LogNotificationAsync(userId, "Document Processing Update", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send document processed notification for {DocumentId}", documentId);
        }
    }

    public async Task SendBulkNotificationAsync(List<Guid> userIds, string subject, string message)
    {
        try
        {
            foreach (var userId in userIds)
            {
                await LogNotificationAsync(userId, subject, message);
            }

            _logger.LogInformation("Sent bulk notification to {UserCount} users with subject '{Subject}'", 
                userIds.Count, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk notification to {UserCount} users", userIds.Count);
        }
    }

    private async Task LogNotificationAsync(Guid userId, string subject, string message)
    {
        // In a real implementation, this would integrate with:
        // - Email service (SendGrid, Amazon SES, etc.)
        // - SMS service (Twilio, etc.)
        // - Push notification service
        // - In-app notification system
        
        _logger.LogInformation("NOTIFICATION for User {UserId} | Subject: {Subject} | Message: {Message}", 
            userId, subject, message);
            
        // Simulate async notification sending
        await Task.Delay(10);
    }
}