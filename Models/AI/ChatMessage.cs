using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace finaid.Models.AI;

/// <summary>
/// Represents a message in a chat conversation
/// </summary>
public class ChatMessage
{
    [Required]
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public Guid? UserId { get; set; }
    
    public Guid? SessionId { get; set; }
    
    /// <summary>
    /// Token count for this message (for context management)
    /// </summary>
    public int? TokenCount { get; set; }
    
    /// <summary>
    /// Message metadata
    /// </summary>
    public MessageMetadata? Metadata { get; set; }
    
    /// <summary>
    /// Whether this message contains sensitive information
    /// </summary>
    public bool ContainsSensitiveData { get; set; } = false;
    
    /// <summary>
    /// Message processing status
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    
    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Additional metadata for chat messages
/// </summary>
public class MessageMetadata
{
    /// <summary>
    /// Source of the message (form field, chat input, etc.)
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Context information when message was sent
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
    
    /// <summary>
    /// Suggested actions based on the message
    /// </summary>
    public List<string> SuggestedActions { get; set; } = new();
    
    /// <summary>
    /// Confidence level of AI response (0.0 to 1.0)
    /// </summary>
    public double? ConfidenceLevel { get; set; }
    
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public int? ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Model version used for this response
    /// </summary>
    public string? ModelVersion { get; set; }
}

/// <summary>
/// Status of a chat message
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message is being composed/typed
    /// </summary>
    Composing,
    
    /// <summary>
    /// Message has been sent
    /// </summary>
    Sent,
    
    /// <summary>
    /// Message is being processed by AI
    /// </summary>
    Processing,
    
    /// <summary>
    /// Message processing completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Message processing failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// Message was edited after sending
    /// </summary>
    Edited,
    
    /// <summary>
    /// Message was deleted
    /// </summary>
    Deleted
}

/// <summary>
/// Helper class for creating common chat messages
/// </summary>
public static class ChatMessageFactory
{
    public static ChatMessage CreateUserMessage(string content, Guid? userId = null, Guid? sessionId = null)
    {
        return new ChatMessage
        {
            Role = "user",
            Content = content,
            UserId = userId,
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow,
            Status = MessageStatus.Sent
        };
    }
    
    public static ChatMessage CreateAssistantMessage(string content, Guid? sessionId = null, MessageMetadata? metadata = null)
    {
        return new ChatMessage
        {
            Role = "assistant",
            Content = content,
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow,
            Status = MessageStatus.Completed,
            Metadata = metadata
        };
    }
    
    public static ChatMessage CreateSystemMessage(string content, Guid? sessionId = null)
    {
        return new ChatMessage
        {
            Role = "system",
            Content = content,
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow,
            Status = MessageStatus.Completed
        };
    }
    
    public static ChatMessage CreateErrorMessage(string errorContent, string? originalContent = null, Guid? sessionId = null)
    {
        return new ChatMessage
        {
            Role = "assistant",
            Content = errorContent,
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow,
            Status = MessageStatus.Failed,
            ErrorMessage = originalContent
        };
    }
}