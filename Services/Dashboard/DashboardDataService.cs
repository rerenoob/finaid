using finaid.Data;
using finaid.Models.Dashboard;
using finaid.Models.User;
using finaid.Services.Progress;
using Microsoft.EntityFrameworkCore;

namespace finaid.Services.Dashboard;

public class DashboardDataService : IDashboardDataService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardDataService> _logger;
    private readonly ProgressCalculationService _progressService;

    public DashboardDataService(ApplicationDbContext context, ILogger<DashboardDataService> logger, ProgressCalculationService progressService)
    {
        _context = context;
        _logger = logger;
        _progressService = progressService;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(Guid userId)
    {
        try
        {
            // For now, we'll create mock data since the full database schema isn't complete
            // In production, this would query the actual database
            
            var applications = await GetApplicationProgressAsync(userId);
            var deadlines = await GetUpcomingDeadlinesAsync(userId);
            var activities = await GetRecentActivitiesAsync(userId);
            var notifications = await GetNotificationSummaryAsync(userId);
            var quickActions = await GetQuickActionsAsync(userId);

            return new DashboardViewModel
            {
                Applications = applications,
                UpcomingDeadlines = deadlines,
                RecentActivities = activities,
                Notifications = notifications,
                QuickActions = quickActions,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard data for user {UserId}", userId);
            return new DashboardViewModel(); // Return empty data on error
        }
    }

    public async Task<List<ApplicationProgress>> GetApplicationProgressAsync(Guid userId)
    {
        // Mock data - replace with actual database queries
        await Task.Delay(100); // Simulate async operation

        // Get progress steps from the progress service
        var fafsaSteps = _progressService.GetFAFSAProgressSteps();
        var stateAidSteps = _progressService.GetStateAidProgressSteps();

        // Calculate progress for each application
        var fafsaProgress = _progressService.CalculateOverallProgress(fafsaSteps);
        var stateAidProgress = _progressService.CalculateOverallProgress(stateAidSteps);

        // Determine application statuses
        var fafsaStatus = _progressService.DetermineApplicationStatus(fafsaSteps, fafsaProgress);
        var stateAidStatus = _progressService.DetermineApplicationStatus(stateAidSteps, stateAidProgress);

        // Get blocking issues
        var fafsaBlockingIssues = _progressService.GetBlockingIssues(fafsaSteps);
        var stateAidBlockingIssues = _progressService.GetBlockingIssues(stateAidSteps);

        return new List<ApplicationProgress>
        {
            new ApplicationProgress
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationType = "FAFSA",
                Title = "Federal Student Aid Application",
                OverallCompletion = fafsaProgress,
                Status = MapToApplicationStatus(fafsaStatus),
                LastUpdated = DateTime.UtcNow.AddDays(-2),
                NextDeadline = _progressService.GetNextDeadline("FAFSA"),
                NextStepDescription = GetNextStepDescription(fafsaSteps),
                HasBlockingIssues = fafsaBlockingIssues.Any(),
                BlockingIssueCount = fafsaBlockingIssues.Count
            },
            new ApplicationProgress
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationType = "StateAid",
                Title = "State Grant Application",
                OverallCompletion = stateAidProgress,
                Status = MapToApplicationStatus(stateAidStatus),
                LastUpdated = DateTime.UtcNow.AddDays(-5),
                NextDeadline = _progressService.GetNextDeadline("StateAid"),
                NextStepDescription = GetNextStepDescription(stateAidSteps),
                HasBlockingIssues = stateAidBlockingIssues.Any(),
                BlockingIssueCount = stateAidBlockingIssues.Count
            }
        };
    }

    public async Task<List<UpcomingDeadline>> GetUpcomingDeadlinesAsync(Guid userId, int daysAhead = 30)
    {
        await Task.Delay(50);

        return new List<UpcomingDeadline>
        {
            new UpcomingDeadline
            {
                Id = Guid.NewGuid(),
                Title = "FAFSA Submission",
                Description = "Complete and submit your FAFSA application",
                DueDate = DateTime.UtcNow.AddDays(15),
                ApplicationType = "FAFSA",
                Priority = DeadlinePriority.High,
                DaysUntilDue = 15,
                ActionUrl = "/fafsa"
            },
            new UpcomingDeadline
            {
                Id = Guid.NewGuid(),
                Title = "Document Upload",
                Description = "Upload required tax documents",
                DueDate = DateTime.UtcNow.AddDays(7),
                ApplicationType = "StateAid",
                Priority = DeadlinePriority.Critical,
                DaysUntilDue = 7,
                ActionUrl = "/documents"
            }
        };
    }

    public async Task<List<RecentActivity>> GetRecentActivitiesAsync(Guid userId, int count = 10)
    {
        await Task.Delay(50);

        return new List<RecentActivity>
        {
            new RecentActivity
            {
                Id = Guid.NewGuid(),
                Title = "FAFSA Section Updated",
                Description = "Student demographic information completed",
                Type = ActivityType.FormSubmitted,
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Icon = "bi-file-earmark-check",
                ActionUrl = "/fafsa"
            },
            new RecentActivity
            {
                Id = Guid.NewGuid(),
                Title = "Document Uploaded",
                Description = "W-2 form successfully uploaded",
                Type = ActivityType.DocumentUploaded,
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Icon = "bi-upload",
                ActionUrl = "/documents"
            },
            new RecentActivity
            {
                Id = Guid.NewGuid(),
                Title = "Profile Updated",
                Description = "Contact information updated",
                Type = ActivityType.UserAction,
                Timestamp = DateTime.UtcNow.AddDays(-3),
                Icon = "bi-person-check",
                ActionUrl = "/profile"
            }
        };
    }

    public async Task<NotificationSummary> GetNotificationSummaryAsync(Guid userId)
    {
        await Task.Delay(50);

        var notifications = new List<NotificationItem>
        {
            new NotificationItem
            {
                Id = Guid.NewGuid(),
                Title = "Deadline Reminder",
                Message = "FAFSA application due in 15 days",
                Type = NotificationType.Warning,
                Timestamp = DateTime.UtcNow.AddHours(-1),
                IsRead = false,
                ActionUrl = "/fafsa"
            },
            new NotificationItem
            {
                Id = Guid.NewGuid(),
                Title = "Document Processed",
                Message = "Your tax document has been successfully processed",
                Type = NotificationType.Success,
                Timestamp = DateTime.UtcNow.AddHours(-4),
                IsRead = true,
                ActionUrl = "/documents"
            }
        };

        return new NotificationSummary
        {
            UnreadCount = notifications.Count(n => !n.IsRead),
            TotalCount = notifications.Count,
            RecentNotifications = notifications.Take(5).ToList()
        };
    }

    public async Task<QuickActionSummary> GetQuickActionsAsync(Guid userId)
    {
        await Task.Delay(50);

        var actions = new List<QuickAction>
        {
            new QuickAction
            {
                Title = "Continue FAFSA",
                Description = "Resume your FAFSA application",
                Icon = "bi-file-earmark-text",
                Url = "/fafsa",
                CssClass = "btn-primary",
                Priority = 1
            },
            new QuickAction
            {
                Title = "Upload Documents",
                Description = "Add required tax documents",
                Icon = "bi-upload",
                Url = "/documents",
                CssClass = "btn-secondary",
                Priority = 2
            },
            new QuickAction
            {
                Title = "Check Progress",
                Description = "View application progress",
                Icon = "bi-graph-up",
                Url = "/progress",
                CssClass = "btn-outline-primary",
                Priority = 3
            }
        };

        return new QuickActionSummary
        {
            Actions = actions.OrderBy(a => a.Priority).ToList(),
            ShowNewUserActions = true // This would be determined based on user state
        };
    }

    private string GetNextStepDescription(List<finaid.Models.Progress.ProgressStep> steps)
    {
        var nextStep = steps
            .Where(s => s.Status == finaid.Models.Progress.StepStatus.NotStarted || 
                       s.Status == finaid.Models.Progress.StepStatus.InProgress ||
                       s.Status == finaid.Models.Progress.StepStatus.RequiresAction)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        return nextStep?.DisplayName ?? "Review and submit";
    }
    
    private ApplicationStatus MapToApplicationStatus(finaid.Models.Progress.ProgressApplicationStatus progressStatus)
    {
        return progressStatus switch
        {
            finaid.Models.Progress.ProgressApplicationStatus.NotStarted => ApplicationStatus.NotStarted,
            finaid.Models.Progress.ProgressApplicationStatus.InProgress => ApplicationStatus.InProgress,
            finaid.Models.Progress.ProgressApplicationStatus.AwaitingDocuments => ApplicationStatus.AwaitingDocuments,
            finaid.Models.Progress.ProgressApplicationStatus.AwaitingReview => ApplicationStatus.AwaitingReview,
            finaid.Models.Progress.ProgressApplicationStatus.Submitted => ApplicationStatus.Submitted,
            finaid.Models.Progress.ProgressApplicationStatus.Approved => ApplicationStatus.Approved,
            finaid.Models.Progress.ProgressApplicationStatus.Rejected => ApplicationStatus.Rejected,
            finaid.Models.Progress.ProgressApplicationStatus.RequiresAction => ApplicationStatus.RequiresAction,
            _ => ApplicationStatus.NotStarted
        };
    }
}