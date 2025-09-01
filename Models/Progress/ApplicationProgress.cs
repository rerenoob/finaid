namespace finaid.Models.Progress;

public class ApplicationProgress
{
    public Guid ApplicationId { get; set; }
    public string ApplicationType { get; set; } = null!; // "FAFSA", "StateAid", "Scholarship"
    public decimal OverallCompletion { get; set; }
    public ProgressApplicationStatus Status { get; set; }
    public List<ProgressStep> Steps { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public DateTime? NextDeadline { get; set; }
    public List<string> BlockingIssues { get; set; } = new();
}

public class ProgressStep
{
    public string StepName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public StepStatus Status { get; set; }
    public decimal Completion { get; set; }
    public bool IsOptional { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> RequiredActions { get; set; } = new();
    public string? Description { get; set; }
    public int Order { get; set; }
}

public enum ProgressApplicationStatus
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

public enum StepStatus
{
    NotStarted,
    InProgress,
    Completed,
    Skipped,
    RequiresAction,
    Blocked
}