using System.ComponentModel.DataAnnotations;

namespace finaid.Models.AI;

/// <summary>
/// Represents an active conversation session with the AI assistant
/// </summary>
public class ConversationSession
{
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public string SessionType { get; set; } = string.Empty; // "fafsa", "general", "document", "eligibility"
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether the session is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Total number of messages in this session
    /// </summary>
    public int MessageCount { get; set; } = 0;
    
    /// <summary>
    /// Total tokens used in this session
    /// </summary>
    public int TotalTokensUsed { get; set; } = 0;
    
    /// <summary>
    /// Session title/name (can be AI-generated)
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Conversation messages in chronological order
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// Current conversation context
    /// </summary>
    public ConversationContext Context { get; set; } = new();
    
    /// <summary>
    /// When the session expires if inactive
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    
    /// <summary>
    /// Session metadata
    /// </summary>
    public SessionMetadata Metadata { get; set; } = new();
    
    /// <summary>
    /// Whether this session should be persisted to database
    /// </summary>
    public bool ShouldPersist { get; set; } = true;
    
    /// <summary>
    /// IP address when session was created (for audit)
    /// </summary>
    public string? ClientIPAddress { get; set; }
    
    /// <summary>
    /// User agent when session was created
    /// </summary>
    public string? ClientUserAgent { get; set; }
}

/// <summary>
/// Context information for a conversation session
/// </summary>
public class ConversationContext
{
    /// <summary>
    /// Current form data being worked on
    /// </summary>
    public Dictionary<string, object> FormData { get; set; } = new();
    
    /// <summary>
    /// Completed form sections
    /// </summary>
    public List<string> CompletedSections { get; set; } = new();
    
    /// <summary>
    /// Current form section being worked on
    /// </summary>
    public string? CurrentSection { get; set; }
    
    /// <summary>
    /// User preferences and settings
    /// </summary>
    public Dictionary<string, string> UserPreferences { get; set; } = new();
    
    /// <summary>
    /// Recent topics discussed in the conversation
    /// </summary>
    public List<string> RecentTopics { get; set; } = new();
    
    /// <summary>
    /// Important information extracted from the conversation
    /// </summary>
    public Dictionary<string, object> ExtractedInformation { get; set; } = new();
    
    /// <summary>
    /// Pending actions or reminders for the user
    /// </summary>
    public List<PendingAction> PendingActions { get; set; } = new();
    
    /// <summary>
    /// Form validation issues that need attention
    /// </summary>
    public List<ValidationIssue> ValidationIssues { get; set; } = new();
    
    /// <summary>
    /// User's intent or goal for this session
    /// </summary>
    public string? SessionGoal { get; set; }
    
    /// <summary>
    /// Progress towards completing the session goal
    /// </summary>
    public double GoalProgress { get; set; } = 0.0; // 0.0 to 1.0
}

/// <summary>
/// Metadata about a conversation session
/// </summary>
public class SessionMetadata
{
    /// <summary>
    /// Browser/platform information
    /// </summary>
    public string? Platform { get; set; }
    
    /// <summary>
    /// Screen size or device type
    /// </summary>
    public string? DeviceType { get; set; }
    
    /// <summary>
    /// Language preference
    /// </summary>
    public string Language { get; set; } = "en-US";
    
    /// <summary>
    /// Timezone of the user
    /// </summary>
    public string? TimeZone { get; set; }
    
    /// <summary>
    /// User's experience level (beginner, intermediate, advanced)
    /// </summary>
    public string? ExperienceLevel { get; set; }
    
    /// <summary>
    /// Accessibility needs or preferences
    /// </summary>
    public List<string> AccessibilityNeeds { get; set; } = new();
    
    /// <summary>
    /// Custom metadata for extensions
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = new();
}

/// <summary>
/// A pending action for the user
/// </summary>
public class PendingAction
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Type { get; set; } = string.Empty; // "reminder", "task", "document_needed", etc.
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? ActionUrl { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public string Priority { get; set; } = "medium"; // "low", "medium", "high", "urgent"
    
    public bool IsCompleted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// A validation issue identified in form data
/// </summary>
public class ValidationIssue
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string FieldName { get; set; } = string.Empty;
    
    [Required]
    public string IssueType { get; set; } = string.Empty; // "missing", "invalid", "inconsistent", "warning"
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? Suggestion { get; set; }
    
    public string Severity { get; set; } = "medium"; // "low", "medium", "high", "critical"
    
    public bool IsResolved { get; set; } = false;
    
    public DateTime IdentifiedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Session statistics for monitoring and optimization
/// </summary>
public class SessionStatistics
{
    public TimeSpan Duration { get; set; }
    public int MessageCount { get; set; }
    public int TokensUsed { get; set; }
    public double AverageResponseTime { get; set; }
    public int ErrorCount { get; set; }
    public string MostDiscussedTopic { get; set; } = string.Empty;
    public double UserSatisfactionScore { get; set; } = 0.0;
    public bool GoalAchieved { get; set; } = false;
}