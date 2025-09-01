namespace finaid.Services.Notifications;

public interface IDocumentNotificationService
{
    Task SendDocumentApprovedNotificationAsync(Guid documentId, Guid userId);
    Task SendDocumentRejectedNotificationAsync(Guid documentId, Guid userId, string reason, List<string> corrections);
    Task SendDocumentReviewRequiredNotificationAsync(Guid documentId, Guid userId);
    Task SendDocumentProcessedNotificationAsync(Guid documentId, Guid userId, bool wasSuccessful);
    Task SendBulkNotificationAsync(List<Guid> userIds, string subject, string message);
}