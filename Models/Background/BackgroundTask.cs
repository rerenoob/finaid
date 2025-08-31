using System.ComponentModel.DataAnnotations;
using finaid.Models;

namespace finaid.Models.Background;

/// <summary>
/// Background task definition
/// </summary>
/// <typeparam name="T">Type of task data</typeparam>
public class BackgroundTask<T> where T : class
{
    [Required]
    public string TaskType { get; set; } = string.Empty;

    [Required]
    public T Data { get; set; } = default!;

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public int MaxRetries { get; set; } = 3;

    public TimeSpan? Timeout { get; set; }

    public string? UserId { get; set; }

    public string? CorrelationId { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Background task status tracking
/// </summary>
public class BackgroundTaskStatus : BaseEntity
{
    [Required]
    public string TaskId { get; set; } = string.Empty;

    [Required]
    public string TaskType { get; set; } = string.Empty;

    public TaskState State { get; set; } = TaskState.Queued;

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public string? UserId { get; set; }

    public string? CorrelationId { get; set; }

    /// <summary>
    /// Serialized task data
    /// </summary>
    public string TaskDataJson { get; set; } = string.Empty;

    /// <summary>
    /// Serialized task result (if completed)
    /// </summary>
    public string? ResultJson { get; set; }

    /// <summary>
    /// Error message if task failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if task failed
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// When the task was queued
    /// </summary>
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the task started processing
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the task completed (successfully or failed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Scheduled execution time (for delayed tasks)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// CRON expression for recurring tasks
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Next execution time for recurring tasks
    /// </summary>
    public DateTime? NextExecution { get; set; }

    /// <summary>
    /// Current retry attempt (0-based)
    /// </summary>
    public int RetryAttempt { get; set; } = 0;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Task timeout duration
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    public long? ProcessingTimeMs { get; set; }

    /// <summary>
    /// Worker/processor that handled this task
    /// </summary>
    public string? ProcessorId { get; set; }

    /// <summary>
    /// Additional task metadata
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Progress percentage (0-100) for long-running tasks
    /// </summary>
    public int ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// Progress message for long-running tasks
    /// </summary>
    public string? ProgressMessage { get; set; }
}

/// <summary>
/// Task execution states
/// </summary>
public enum TaskState
{
    /// <summary>
    /// Task is queued and waiting to be processed
    /// </summary>
    Queued,

    /// <summary>
    /// Task is scheduled for future execution
    /// </summary>
    Scheduled,

    /// <summary>
    /// Task is currently being processed
    /// </summary>
    Processing,

    /// <summary>
    /// Task completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Task failed and will be retried
    /// </summary>
    Retrying,

    /// <summary>
    /// Task failed and exceeded max retries
    /// </summary>
    Failed,

    /// <summary>
    /// Task was cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Task timed out during execution
    /// </summary>
    TimedOut
}

/// <summary>
/// Task priority levels
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Context provided to task processors
/// </summary>
public class TaskExecutionContext
{
    public string TaskId { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public int RetryAttempt { get; set; }
    public int MaxRetries { get; set; }
    public string? UserId { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public CancellationToken CancellationToken { get; set; }
    
    /// <summary>
    /// Reports progress for long-running tasks
    /// </summary>
    /// <param name="percentage">Progress percentage (0-100)</param>
    /// <param name="message">Progress message</param>
    public Action<int, string>? ReportProgress { get; set; }
}

/// <summary>
/// Result of background task execution
/// </summary>
public class BackgroundTaskResult
{
    public bool IsSuccess { get; set; }
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static BackgroundTaskResult Success(object? data = null)
    {
        return new BackgroundTaskResult
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static BackgroundTaskResult Failure(string errorMessage, string? stackTrace = null)
    {
        return new BackgroundTaskResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorStackTrace = stackTrace
        };
    }

    public static BackgroundTaskResult Failure(Exception exception)
    {
        return new BackgroundTaskResult
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            ErrorStackTrace = exception.StackTrace
        };
    }
}

/// <summary>
/// Queue statistics for monitoring
/// </summary>
public class QueueStatistics
{
    public int QueuedTasks { get; set; }
    public int ProcessingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int ScheduledTasks { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, int> TaskTypeDistribution { get; set; } = new();
    public Dictionary<TaskPriority, int> PriorityDistribution { get; set; } = new();
}