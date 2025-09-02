using finaid.Models.AI;

namespace finaid.Services.AI;

/// <summary>
/// Mock implementation of conversation context service for development
/// Uses in-memory storage instead of Redis
/// </summary>
public class MockConversationContextService : IConversationContextService
{
    private readonly Dictionary<Guid, ConversationSession> _sessions = new();
    private readonly IAIAssistantService _aiAssistantService;
    private readonly ILogger<MockConversationContextService> _logger;

    public MockConversationContextService(
        IAIAssistantService aiAssistantService,
        ILogger<MockConversationContextService> logger)
    {
        _aiAssistantService = aiAssistantService ?? throw new ArgumentNullException(nameof(aiAssistantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ConversationSession> CreateSessionAsync(Guid userId, string sessionType, CancellationToken cancellationToken = default)
    {
        return CreateSessionWithIdAsync(Guid.NewGuid(), userId, sessionType, cancellationToken);
    }

    /// <summary>
    /// Create a session with a specific ID (for mock/development purposes)
    /// </summary>
    public Task<ConversationSession> CreateSessionWithIdAsync(Guid sessionId, Guid userId, string sessionType, CancellationToken cancellationToken = default)
    {
        var session = new ConversationSession
        {
            Id = sessionId,
            UserId = userId,
            SessionType = sessionType,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            IsActive = true,
            MessageCount = 0,
            TotalTokensUsed = 0,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            ShouldPersist = false // Don't persist mock sessions
        };

        _sessions[session.Id] = session;
        _logger.LogInformation("Created mock conversation session {SessionId} for user {UserId}", session.Id, userId);
        
        return Task.FromResult(session);
    }

    public Task<ConversationSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<List<ConversationSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userSessions = _sessions.Values
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivity)
            .ToList();
        
        return Task.FromResult(userSessions);
    }

    public Task<bool> UpdateContextAsync(Guid sessionId, ConversationContext context, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Context = context;
            session.LastActivity = DateTime.UtcNow;
            _logger.LogDebug("Updated context for session {SessionId}", sessionId);
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }

    public Task<bool> AddMessageAsync(Guid sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            message.SessionId = sessionId;
            session.Messages.Add(message);
            session.MessageCount = session.Messages.Count;
            session.LastActivity = DateTime.UtcNow;
            
            // Estimate token usage (simple approximation: 1 token per 4 characters)
            if (message.TokenCount.HasValue)
            {
                session.TotalTokensUsed += message.TokenCount.Value;
            }
            else
            {
                var estimatedTokens = message.Content.Length / 4;
                message.TokenCount = estimatedTokens;
                session.TotalTokensUsed += estimatedTokens;
            }
            
            _logger.LogDebug("Added message to session {SessionId}. Total messages: {MessageCount}", 
                sessionId, session.MessageCount);
            
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }

    public Task<List<ChatMessage>> GetOptimizedContextAsync(Guid sessionId, int maxTokens, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(new List<ChatMessage>());
        }

        // Simple token optimization: take recent messages up to token limit
        var messages = new List<ChatMessage>();
        var currentTokens = 0;
        
        // Add messages in reverse order (most recent first) until we hit the limit
        for (int i = session.Messages.Count - 1; i >= 0; i--)
        {
            var message = session.Messages[i];
            var messageTokens = message.TokenCount ?? (message.Content.Length / 4);
            
            if (currentTokens + messageTokens > maxTokens && messages.Any())
            {
                break;
            }
            
            messages.Insert(0, message); // Insert at beginning to maintain chronological order
            currentTokens += messageTokens;
        }
        
        _logger.LogDebug("Retrieved {MessageCount} messages ({TokenCount} tokens) for session {SessionId}", 
            messages.Count, currentTokens, sessionId);
        
        return Task.FromResult(messages);
    }

    public Task<bool> ClearSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Messages.Clear();
            session.MessageCount = 0;
            session.TotalTokensUsed = 0;
            session.LastActivity = DateTime.UtcNow;
            
            _logger.LogInformation("Cleared session {SessionId}", sessionId);
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }

    public Task<bool> UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastActivity = DateTime.UtcNow;
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }

    public Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredSessions = _sessions.Values
            .Where(s => s.ExpiresAt < DateTime.UtcNow)
            .ToList();
        
        foreach (var session in expiredSessions)
        {
            _sessions.Remove(session.Id);
        }
        
        _logger.LogInformation("Cleaned up {ExpiredCount} expired sessions", expiredSessions.Count);
        return Task.FromResult(expiredSessions.Count);
    }

    public Task<SessionStatistics?> GetSessionStatisticsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult<SessionStatistics?>(null);
        }

        var stats = new SessionStatistics
        {
            Duration = DateTime.UtcNow - session.CreatedAt,
            MessageCount = session.MessageCount,
            TokensUsed = session.TotalTokensUsed,
            AverageResponseTime = 1.5, // Mock average
            ErrorCount = session.Messages.Count(m => m.Status == MessageStatus.Failed),
            MostDiscussedTopic = "Financial Aid", // Mock
            UserSatisfactionScore = 0.85, // Mock
            GoalAchieved = session.Context.GoalProgress >= 1.0
        };
        
        return Task.FromResult<SessionStatistics?>(stats);
    }

    public Task<string?> GenerateSessionSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult<string?>(null);
        }

        if (!session.Messages.Any())
        {
            return Task.FromResult<string?>("No messages in this conversation.");
        }

        // Simple summary generation
        var userMessages = session.Messages.Count(m => m.Role == "user");
        var aiMessages = session.Messages.Count(m => m.Role == "assistant");
        var duration = DateTime.UtcNow - session.CreatedAt;
        
        var summary = $"Conversation Summary:\n" +
                     $"- Duration: {duration.TotalMinutes:F0} minutes\n" +
                     $"- User messages: {userMessages}\n" +
                     $"- AI responses: {aiMessages}\n" +
                     $"- Session type: {session.SessionType}\n" +
                     $"- Total tokens: {session.TotalTokensUsed}";
        
        return Task.FromResult<string?>(summary);
    }

    public Task<bool> ArchiveSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsActive = false;
            _logger.LogInformation("Archived session {SessionId}", sessionId);
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }
}