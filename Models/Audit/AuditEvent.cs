using System.ComponentModel.DataAnnotations;
using finaid.Models;

namespace finaid.Models.Audit;

/// <summary>
/// Audit event entity for compliance logging
/// </summary>
public class AuditEvent : BaseEntity
{
    [Required]
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public AuditEventType EventType { get; set; }

    [Required]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public string EventDescription { get; set; } = string.Empty;

    /// <summary>
    /// User who performed the action (if applicable)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Student affected by the action (if applicable)
    /// </summary>
    public string? StudentId { get; set; }

    /// <summary>
    /// IP address of the user
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// System component that generated the event
    /// </summary>
    [Required]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// Event severity
    /// </summary>
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;

    /// <summary>
    /// Additional event details as JSON
    /// </summary>
    public string? EventDetails { get; set; }

    /// <summary>
    /// Request ID for correlation with other logs
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Session ID for user session tracking
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Resource accessed (URL, API endpoint, etc.)
    /// </summary>
    public string? ResourceAccessed { get; set; }

    /// <summary>
    /// HTTP method used (for API calls)
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// HTTP response status code (for API calls)
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Response time in milliseconds (for API calls)
    /// </summary>
    public long? ResponseTimeMs { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this event contains sensitive data that should be encrypted
    /// </summary>
    public bool ContainsSensitiveData { get; set; } = false;

    /// <summary>
    /// Compliance flags for regulatory requirements
    /// </summary>
    public ComplianceFlags ComplianceFlags { get; set; } = ComplianceFlags.None;
}

/// <summary>
/// Types of audit events
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// User authentication events
    /// </summary>
    Authentication,

    /// <summary>
    /// Authorization and access control events
    /// </summary>
    Authorization,

    /// <summary>
    /// Data access events (FERPA compliance)
    /// </summary>
    DataAccess,

    /// <summary>
    /// API calls to external systems
    /// </summary>
    ApiCall,

    /// <summary>
    /// Data modification events
    /// </summary>
    DataModification,

    /// <summary>
    /// Security events (breaches, suspicious activity)
    /// </summary>
    Security,

    /// <summary>
    /// System events (startup, shutdown, errors)
    /// </summary>
    System,

    /// <summary>
    /// Configuration changes
    /// </summary>
    Configuration,

    /// <summary>
    /// File uploads and downloads
    /// </summary>
    FileOperation,

    /// <summary>
    /// Email notifications sent
    /// </summary>
    Notification,

    /// <summary>
    /// Batch operations
    /// </summary>
    BatchOperation,

    /// <summary>
    /// Compliance-related events
    /// </summary>
    Compliance
}

/// <summary>
/// Data access types for FERPA compliance
/// </summary>
public enum DataAccessType
{
    /// <summary>
    /// Viewing data
    /// </summary>
    Read,

    /// <summary>
    /// Creating new data
    /// </summary>
    Create,

    /// <summary>
    /// Updating existing data
    /// </summary>
    Update,

    /// <summary>
    /// Deleting data
    /// </summary>
    Delete,

    /// <summary>
    /// Exporting data
    /// </summary>
    Export,

    /// <summary>
    /// Printing data
    /// </summary>
    Print,

    /// <summary>
    /// Searching data
    /// </summary>
    Search
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    /// <summary>
    /// Successful login
    /// </summary>
    LoginSuccess,

    /// <summary>
    /// Failed login attempt
    /// </summary>
    LoginFailure,

    /// <summary>
    /// User logout
    /// </summary>
    Logout,

    /// <summary>
    /// Account locked
    /// </summary>
    AccountLocked,

    /// <summary>
    /// Password changed
    /// </summary>
    PasswordChanged,

    /// <summary>
    /// Unauthorized access attempt
    /// </summary>
    UnauthorizedAccess,

    /// <summary>
    /// Suspicious activity detected
    /// </summary>
    SuspiciousActivity,

    /// <summary>
    /// Data breach detected
    /// </summary>
    DataBreach,

    /// <summary>
    /// Security configuration changed
    /// </summary>
    SecurityConfigChange,

    /// <summary>
    /// Multi-factor authentication event
    /// </summary>
    MultiFactorAuth
}

/// <summary>
/// System event types
/// </summary>
public enum SystemEventType
{
    /// <summary>
    /// Application startup
    /// </summary>
    Startup,

    /// <summary>
    /// Application shutdown
    /// </summary>
    Shutdown,

    /// <summary>
    /// System error
    /// </summary>
    Error,

    /// <summary>
    /// Performance issue
    /// </summary>
    Performance,

    /// <summary>
    /// Database connection event
    /// </summary>
    Database,

    /// <summary>
    /// External API connection event
    /// </summary>
    ExternalApi,

    /// <summary>
    /// File system event
    /// </summary>
    FileSystem,

    /// <summary>
    /// Maintenance event
    /// </summary>
    Maintenance,

    /// <summary>
    /// Backup operation
    /// </summary>
    Backup,

    /// <summary>
    /// Configuration reload
    /// </summary>
    ConfigurationReload
}

/// <summary>
/// Audit event severity levels
/// </summary>
public enum AuditSeverity
{
    /// <summary>
    /// Informational events
    /// </summary>
    Information = 0,

    /// <summary>
    /// Warning events
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error events
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical events requiring immediate attention
    /// </summary>
    Critical = 3
}

/// <summary>
/// Compliance flags for regulatory requirements
/// </summary>
[Flags]
public enum ComplianceFlags
{
    /// <summary>
    /// No compliance requirements
    /// </summary>
    None = 0,

    /// <summary>
    /// FERPA compliance required
    /// </summary>
    FERPA = 1,

    /// <summary>
    /// GLBA compliance required
    /// </summary>
    GLBA = 2,

    /// <summary>
    /// CCPA compliance required
    /// </summary>
    CCPA = 4,

    /// <summary>
    /// SOX compliance required
    /// </summary>
    SOX = 8,

    /// <summary>
    /// PCI DSS compliance required
    /// </summary>
    PCIDSS = 16,

    /// <summary>
    /// HIPAA compliance required
    /// </summary>
    HIPAA = 32
}