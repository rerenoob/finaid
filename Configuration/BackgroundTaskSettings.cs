using System.ComponentModel.DataAnnotations;

namespace finaid.Configuration;

/// <summary>
/// Background task service configuration
/// </summary>
public class BackgroundTaskSettings
{
    public const string SectionName = "BackgroundTasks";

    /// <summary>
    /// Maximum number of tasks that can run concurrently
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentTasks { get; set; } = 5;

    /// <summary>
    /// Maximum number of tasks to process in each batch
    /// </summary>
    [Range(1, 1000)]
    public int MaxTasksPerBatch { get; set; } = 10;

    /// <summary>
    /// Interval in seconds between queue processing cycles
    /// </summary>
    [Range(1, 3600)]
    public int ProcessingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Interval in seconds between scheduled task checks
    /// </summary>
    [Range(1, 3600)]
    public int ScheduledTaskCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Base delay in seconds before first retry attempt
    /// </summary>
    [Range(1, 300)]
    public int RetryBaseDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Maximum delay in seconds between retry attempts
    /// </summary>
    [Range(60, 86400)]
    public int MaxRetryDelaySeconds { get; set; } = 300;

    /// <summary>
    /// Enable automatic cleanup of completed tasks
    /// </summary>
    public bool AutoCleanupEnabled { get; set; } = true;

    /// <summary>
    /// How long to retain completed tasks before cleanup
    /// </summary>
    public TimeSpan CompletedTaskRetentionTime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Interval in hours between automatic cleanup runs
    /// </summary>
    [Range(1, 168)]
    public int CleanupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Default timeout for task execution
    /// </summary>
    public TimeSpan DefaultTaskTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Task-specific configurations
    /// </summary>
    public Dictionary<string, TaskTypeSettings> TaskTypes { get; set; } = new()
    {
        ["EligibilityCheck"] = new()
        {
            MaxRetries = 3,
            Timeout = TimeSpan.FromMinutes(5),
            Priority = TaskPriority.High,
            EnableProgressReporting = false
        },
        ["ApplicationSubmission"] = new()
        {
            MaxRetries = 5,
            Timeout = TimeSpan.FromMinutes(15),
            Priority = TaskPriority.Critical,
            EnableProgressReporting = true
        },
        ["DocumentUpload"] = new()
        {
            MaxRetries = 3,
            Timeout = TimeSpan.FromMinutes(10),
            Priority = TaskPriority.High,
            EnableProgressReporting = true
        },
        ["StatusUpdate"] = new()
        {
            MaxRetries = 2,
            Timeout = TimeSpan.FromMinutes(2),
            Priority = TaskPriority.Normal,
            EnableProgressReporting = false
        },
        ["EmailNotification"] = new()
        {
            MaxRetries = 5,
            Timeout = TimeSpan.FromMinutes(2),
            Priority = TaskPriority.Normal,
            EnableProgressReporting = false
        },
        ["DataCleanup"] = new()
        {
            MaxRetries = 1,
            Timeout = TimeSpan.FromHours(1),
            Priority = TaskPriority.Low,
            EnableProgressReporting = true
        }
    };

    /// <summary>
    /// Queue configuration settings
    /// </summary>
    public QueueSettings Queue { get; set; } = new();

    /// <summary>
    /// Monitoring and alerting settings
    /// </summary>
    public TaskMonitoringSettings Monitoring { get; set; } = new();
}

/// <summary>
/// Task priority levels (matches enum in BackgroundTask.cs)
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Configuration for specific task types
/// </summary>
public class TaskTypeSettings
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Task execution timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Task priority level
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    /// <summary>
    /// Enable progress reporting for this task type
    /// </summary>
    public bool EnableProgressReporting { get; set; } = false;

    /// <summary>
    /// Maximum number of concurrent instances of this task type
    /// </summary>
    public int? MaxConcurrentInstances { get; set; }

    /// <summary>
    /// Rate limiting: maximum executions per time window
    /// </summary>
    public int? MaxExecutionsPerHour { get; set; }

    /// <summary>
    /// Whether this task type requires database transaction
    /// </summary>
    public bool RequiresTransaction { get; set; } = false;

    /// <summary>
    /// Custom configuration for this task type
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Queue configuration settings
/// </summary>
public class QueueSettings
{
    /// <summary>
    /// Queue implementation type
    /// </summary>
    public QueueType Type { get; set; } = QueueType.InMemory;

    /// <summary>
    /// Connection string for external queue systems
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Queue name/topic for external systems
    /// </summary>
    public string QueueName { get; set; } = "finaid-background-tasks";

    /// <summary>
    /// Enable dead letter queue for failed messages
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Dead letter queue name
    /// </summary>
    public string DeadLetterQueueName { get; set; } = "finaid-background-tasks-dlq";

    /// <summary>
    /// Message time-to-live in external queue systems
    /// </summary>
    public TimeSpan MessageTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Enable message persistence for external queue systems
    /// </summary>
    public bool EnablePersistence { get; set; } = true;
}

/// <summary>
/// Queue implementation types
/// </summary>
public enum QueueType
{
    /// <summary>
    /// In-memory queue (single instance only)
    /// </summary>
    InMemory,

    /// <summary>
    /// Database-backed queue
    /// </summary>
    Database,

    /// <summary>
    /// Redis-based queue
    /// </summary>
    Redis,

    /// <summary>
    /// Azure Service Bus
    /// </summary>
    ServiceBus,

    /// <summary>
    /// Amazon SQS
    /// </summary>
    AmazonSqs
}

/// <summary>
/// Task monitoring and alerting settings
/// </summary>
public class TaskMonitoringSettings
{
    /// <summary>
    /// Enable performance monitoring
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Enable alerting for task failures
    /// </summary>
    public bool EnableFailureAlerts { get; set; } = true;

    /// <summary>
    /// Enable alerting for long-running tasks
    /// </summary>
    public bool EnableLongRunningTaskAlerts { get; set; } = true;

    /// <summary>
    /// Threshold for considering a task as long-running
    /// </summary>
    public TimeSpan LongRunningTaskThreshold { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Enable alerting for high queue depth
    /// </summary>
    public bool EnableQueueDepthAlerts { get; set; } = true;

    /// <summary>
    /// Queue depth threshold for alerts
    /// </summary>
    public int QueueDepthThreshold { get; set; } = 1000;

    /// <summary>
    /// Enable metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Metrics collection interval
    /// </summary>
    public TimeSpan MetricsInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Health check configuration
    /// </summary>
    public HealthCheckSettings HealthCheck { get; set; } = new();
}

/// <summary>
/// Health check configuration for background tasks
/// </summary>
public class HealthCheckSettings
{
    /// <summary>
    /// Enable background task health checks
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Health check interval
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum allowed queue depth for healthy status
    /// </summary>
    public int MaxHealthyQueueDepth { get; set; } = 500;

    /// <summary>
    /// Maximum allowed failed task percentage for healthy status
    /// </summary>
    public double MaxHealthyFailurePercentage { get; set; } = 5.0;

    /// <summary>
    /// Time window for calculating failure percentage
    /// </summary>
    public TimeSpan FailureCalculationWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum allowed processing time for healthy status
    /// </summary>
    public TimeSpan MaxHealthyProcessingTime { get; set; } = TimeSpan.FromMinutes(30);
}