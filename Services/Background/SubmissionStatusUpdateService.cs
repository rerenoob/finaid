using finaid.Data;
using finaid.Models.Application;
using finaid.Services.FAFSA;
using Microsoft.EntityFrameworkCore;

namespace finaid.Services.Background;

/// <summary>
/// Background service that periodically checks and updates FAFSA submission statuses
/// </summary>
public class SubmissionStatusUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubmissionStatusUpdateService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Check every 30 minutes

    public SubmissionStatusUpdateService(
        IServiceProvider serviceProvider,
        ILogger<SubmissionStatusUpdateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FAFSA Submission Status Update Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateSubmissionStatusesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating submission statuses");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("FAFSA Submission Status Update Service stopped");
    }

    private async Task UpdateSubmissionStatusesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var submissionService = scope.ServiceProvider.GetRequiredService<IFAFSASubmissionService>();

        try
        {
            // Get all applications that have been submitted but not yet completed
            var pendingApplications = await context.Set<FAFSAApplication>()
                .Where(a => a.Status == ApplicationStatus.Submitted || 
                           a.Status == ApplicationStatus.Processing)
                .Where(a => !string.IsNullOrEmpty(a.ConfirmationNumber))
                .Where(a => a.SubmittedAt.HasValue && 
                           a.SubmittedAt.Value > DateTime.UtcNow.AddDays(-30)) // Only check recent submissions
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} pending submissions to check status for", 
                pendingApplications.Count);

            var updated = 0;
            var errors = 0;

            foreach (var application in pendingApplications)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await UpdateSingleApplicationStatusAsync(application, submissionService, context);
                    updated++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update status for application {ApplicationId}", 
                        application.Id);
                    errors++;
                }

                // Small delay between requests to avoid overwhelming the federal API
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            if (updated > 0 || errors > 0)
            {
                _logger.LogInformation("Status update completed. Updated: {Updated}, Errors: {Errors}", 
                    updated, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateSubmissionStatusesAsync");
        }
    }

    private async Task UpdateSingleApplicationStatusAsync(
        FAFSAApplication application, 
        IFAFSASubmissionService submissionService, 
        ApplicationDbContext context)
    {
        if (string.IsNullOrEmpty(application.ConfirmationNumber))
            return;

        // Get current status from federal system
        var currentStatus = await submissionService.GetSubmissionStatusAsync(application.ConfirmationNumber);

        var statusChanged = false;
        var previousStatus = application.Status;

        // Update application status based on federal system status
        switch (currentStatus.Status)
        {
            case Models.FAFSA.SubmissionStatus.Processing:
                if (application.Status != ApplicationStatus.Processing)
                {
                    application.Status = ApplicationStatus.Processing;
                    statusChanged = true;
                }
                break;

            case Models.FAFSA.SubmissionStatus.Processed:
            case Models.FAFSA.SubmissionStatus.Completed:
                if (application.Status != ApplicationStatus.Completed)
                {
                    application.Status = ApplicationStatus.Completed;
                    application.ProcessedAt = currentStatus.ProcessedAt;
                    
                    // Update calculated financial aid results if available
                    if (currentStatus.Results != null)
                    {
                        application.EstimatedEFC = currentStatus.Results.ExpectedFamilyContribution;
                        application.EstimatedPellGrant = currentStatus.Results.PellGrantAmount;
                    }
                    
                    statusChanged = true;
                }
                break;

            case Models.FAFSA.SubmissionStatus.Rejected:
                if (application.Status != ApplicationStatus.Rejected)
                {
                    application.Status = ApplicationStatus.Rejected;
                    
                    // Combine rejection reasons
                    var rejectionReasons = currentStatus.Issues
                        .Where(i => i.Severity == Models.FAFSA.IssueSeverity.Critical)
                        .Select(i => i.Description)
                        .ToList();
                    
                    if (rejectionReasons.Any())
                    {
                        application.RejectionReason = string.Join("; ", rejectionReasons);
                    }
                    
                    statusChanged = true;
                }
                break;

            case Models.FAFSA.SubmissionStatus.VerificationRequired:
                if (application.Status != ApplicationStatus.VerificationRequired)
                {
                    application.Status = ApplicationStatus.VerificationRequired;
                    statusChanged = true;
                }
                break;

            case Models.FAFSA.SubmissionStatus.OnHold:
                if (application.Status != ApplicationStatus.OnHold)
                {
                    application.Status = ApplicationStatus.OnHold;
                    
                    // Update processing notes with hold reason
                    var holdReasons = currentStatus.Issues
                        .Where(i => i.Severity >= Models.FAFSA.IssueSeverity.Medium)
                        .Select(i => i.Description)
                        .ToList();
                    
                    if (holdReasons.Any())
                    {
                        application.ProcessingNotes = "On Hold: " + string.Join("; ", holdReasons);
                    }
                    
                    statusChanged = true;
                }
                break;
        }

        // Always update the last activity timestamp
        application.LastActivityAt = DateTime.UtcNow;

        if (statusChanged)
        {
            _logger.LogInformation("Status updated for application {ApplicationId}: {OldStatus} -> {NewStatus}", 
                application.Id, previousStatus, application.Status);

            // Save changes to database
            await context.SaveChangesAsync();

            // Log the status change for audit purposes
            await LogStatusChangeAsync(application, previousStatus, context);

            // Could trigger notifications here
            await NotifyStatusChangeAsync(application, previousStatus, currentStatus);
        }
    }

    private async Task LogStatusChangeAsync(
        FAFSAApplication application, 
        ApplicationStatus previousStatus, 
        ApplicationDbContext context)
    {
        try
        {
            // Create an audit log entry
            var auditLog = new ApplicationAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                ChangeType = "StatusUpdate",
                OldValue = previousStatus.ToString(),
                NewValue = application.Status.ToString(),
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "System.StatusUpdateService",
                Notes = "Automatic status update from federal system"
            };

            context.Set<ApplicationAuditLog>().Add(auditLog);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log status change for application {ApplicationId}", 
                application.Id);
        }
    }

    private async Task NotifyStatusChangeAsync(
        FAFSAApplication application, 
        ApplicationStatus previousStatus, 
        Models.FAFSA.FAFSASubmissionStatus currentStatus)
    {
        try
        {
            // Here you would implement notification logic:
            // - Send email to user about status change
            // - Send push notifications
            // - Update real-time UI via SignalR
            // - Log to audit trail

            _logger.LogDebug("Notifying user of status change for application {ApplicationId}", 
                application.Id);

            // Placeholder for notification implementation
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify status change for application {ApplicationId}", 
                application.Id);
        }
    }
}

/// <summary>
/// Audit log entry for application changes
/// </summary>
public class ApplicationAuditLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}