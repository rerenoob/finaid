using finaid.Models.AI;

namespace finaid.Services.AI;

/// <summary>
/// Interface for managing conversation context and session state
/// </summary>
public interface IConversationContextService
{
    /// <summary>
    /// Create a new conversation session
    /// </summary>
    /// <param name="userId">User ID who owns the session</param>
    /// <param name="sessionType">Type of session (fafsa, general, document)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New conversation session</returns>
    Task<ConversationSession> CreateSessionAsync(Guid userId, string sessionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an existing conversation session
    /// </summary>
    /// <param name="sessionId">Session ID to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conversation session or null if not found</returns>
    Task<ConversationSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    /// <param name="userId">User ID to get sessions for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    Task<List<ConversationSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update conversation context for a session
    /// </summary>
    /// <param name="sessionId">Session ID to update</param>
    /// <param name="context">Updated context data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure</returns>
    Task<bool> UpdateContextAsync(Guid sessionId, ConversationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a message to a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID to add message to</param>
    /// <param name="message">Message to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure</returns>
    Task<bool> AddMessageAsync(Guid sessionId, ChatMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get optimized conversation context for AI requests
    /// </summary>
    /// <param name="sessionId">Session ID to get context for</param>
    /// <param name="maxTokens">Maximum tokens allowed in context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimized message list</returns>
    Task<List<ChatMessage>> GetOptimizedContextAsync(Guid sessionId, int maxTokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all data for a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID to clear</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure</returns>
    Task<bool> ClearSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update session activity timestamp
    /// </summary>
    /// <param name="sessionId">Session ID to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure</returns>
    Task<bool> UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get session statistics
    /// </summary>
    /// <param name="sessionId">Session ID to get statistics for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session statistics or null if session not found</returns>
    Task<SessionStatistics?> GetSessionStatisticsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a summary of the conversation
    /// </summary>
    /// <param name="sessionId">Session ID to summarize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conversation summary</returns>
    Task<string?> GenerateSessionSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID to archive</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure</returns>
    Task<bool> ArchiveSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}