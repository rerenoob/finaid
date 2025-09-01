using finaid.Models.User;

namespace finaid.Models.Dashboard;

public class DashboardViewModel
{
    public List<ApplicationProgress> Applications { get; set; } = new();
    public List<UpcomingDeadline> UpcomingDeadlines { get; set; } = new();
    public List<RecentActivity> RecentActivities { get; set; } = new();
    public NotificationSummary Notifications { get; set; } = new();
    public QuickActionSummary QuickActions { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ApplicationProgress
{
    public Guid ApplicationId { get; set; }
    public string ApplicationType { get; set; } = string.Empty; // "FAFSA", "StateAid", "Scholarship"
    public string Title { get; set; } = string.Empty;
    public decimal OverallCompletion { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? NextDeadline { get; set; }
    public string NextStepDescription { get; set; } = string.Empty;
    public bool HasBlockingIssues { get; set; }
    public int BlockingIssueCount { get; set; }
}

public class UpcomingDeadline
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string ApplicationType { get; set; } = string.Empty;
    public DeadlinePriority Priority { get; set; }
    public int DaysUntilDue { get; set; }
    public string ActionUrl { get; set; } = string.Empty;
}

public class RecentActivity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ActivityType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
}

public class NotificationSummary
{
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
    public List<NotificationItem> RecentNotifications { get; set; } = new();
}

public class NotificationItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
    public string ActionUrl { get; set; } = string.Empty;
}

public class QuickActionSummary
{
    public List<QuickAction> Actions { get; set; } = new();
    public bool ShowNewUserActions { get; set; }
}

public class QuickAction
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string CssClass { get; set; } = "btn-primary";
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; }
}

public enum ApplicationStatus
{
    NotStarted,
    InProgress,
    AwaitingDocuments,
    AwaitingReview,
    Submitted,
    Approved,
    Rejected,
    RequiresAction
}

public enum DeadlinePriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ActivityType
{
    FormSubmitted,
    DocumentUploaded,
    ApplicationStatusChanged,
    DeadlineReminder,
    SystemNotification,
    UserAction
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Reminder
}