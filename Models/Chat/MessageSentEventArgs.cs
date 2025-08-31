namespace finaid.Models.Chat;

/// <summary>
/// Event arguments for when a message is sent from the chat input
/// </summary>
public class MessageSentEventArgs
{
    /// <summary>
    /// The message content
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Whether to include form context with the message
    /// </summary>
    public bool IncludeFormContext { get; }
    
    /// <summary>
    /// Create new message sent event args
    /// </summary>
    /// <param name="message">The message content</param>
    /// <param name="includeFormContext">Whether to include form context</param>
    public MessageSentEventArgs(string message, bool includeFormContext = false)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        IncludeFormContext = includeFormContext;
    }
}