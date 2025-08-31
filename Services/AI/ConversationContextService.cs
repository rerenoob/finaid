using finaid.Data;
using finaid.Models.AI;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace finaid.Services.AI;

/// <summary>
/// Service for managing conversation context and session state with Redis caching
/// </summary>
public class ConversationContextService : IConversationContextService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDatabase _redis;
    private readonly IAIAssistantService _aiAssistantService;
    private readonly ILogger<ConversationContextService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Redis key patterns
    private const string SESSION_KEY_PREFIX = "conversation:session:";
    private const string USER_SESSIONS_KEY_PREFIX = "conversation:user:";
    private const string SESSION_MESSAGES_KEY_PREFIX = "conversation:messages:";
    private const string SESSION_CONTEXT_KEY_PREFIX = "conversation:context:";

    // Cache expiration times
    private readonly TimeSpan _sessionCacheExpiry = TimeSpan.FromHours(6);
    private readonly TimeSpan _messagesCacheExpiry = TimeSpan.FromHours(2);
    private readonly TimeSpan _contextCacheExpiry = TimeSpan.FromHours(1);

    public ConversationContextService(
        ApplicationDbContext dbContext,
        IConnectionMultiplexer redis,
        IAIAssistantService aiAssistantService,
        ILogger<ConversationContextService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _redis = redis?.GetDatabase() ?? throw new ArgumentNullException(nameof(redis));
        _aiAssistantService = aiAssistantService ?? throw new ArgumentNullException(nameof(aiAssistantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<ConversationSession> CreateSessionAsync(Guid userId, string sessionType, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = new ConversationSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionType = sessionType,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Title = $"New {sessionType.ToTitleCase()} Session"
            };

            // Store in Redis cache
            var sessionKey = GetSessionKey(session.Id);
            var sessionJson = JsonSerializer.Serialize(session, _jsonOptions);
            await _redis.StringSetAsync(sessionKey, sessionJson, _sessionCacheExpiry);

            // Add to user's session list
            var userSessionsKey = GetUserSessionsKey(userId);
            await _redis.SetAddAsync(userSessionsKey, session.Id.ToString());
            await _redis.KeyExpireAsync(userSessionsKey, _sessionCacheExpiry);

            // Persist to database if configured to do so
            if (session.ShouldPersist)
            {
                await PersistSessionToDbAsync(session);
            }

            _logger.LogInformation("Created new conversation session {SessionId} for user {UserId} of type {SessionType}",
                session.Id, userId, sessionType);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create conversation session for user {UserId} of type {SessionType}",
                userId, sessionType);
            throw;
        }
    }

    public async Task<ConversationSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get from Redis cache first
            var sessionKey = GetSessionKey(sessionId);
            var sessionJson = await _redis.StringGetAsync(sessionKey);

            ConversationSession? session = null;

            if (sessionJson.HasValue)
            {
                session = JsonSerializer.Deserialize<ConversationSession>(sessionJson!, _jsonOptions);
            }
            else
            {
                // Fallback to database
                session = await GetSessionFromDbAsync(sessionId, cancellationToken);
                
                if (session != null)
                {
                    // Cache the session for future requests
                    var serialized = JsonSerializer.Serialize(session, _jsonOptions);
                    await _redis.StringSetAsync(sessionKey, serialized, _sessionCacheExpiry);
                }
            }

            if (session != null)
            {
                // Load messages and context
                session.Messages = await GetSessionMessagesAsync(sessionId);
                session.Context = await GetSessionContextAsync(sessionId);

                // Update activity timestamp
                await UpdateActivityAsync(sessionId, cancellationToken);
            }

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conversation session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<List<ConversationSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = new List<ConversationSession>();
            var userSessionsKey = GetUserSessionsKey(userId);
            var sessionIds = await _redis.SetMembersAsync(userSessionsKey);

            foreach (var sessionIdValue in sessionIds)
            {
                if (Guid.TryParse(sessionIdValue, out var sessionId))
                {
                    var session = await GetSessionAsync(sessionId, cancellationToken);
                    if (session != null && session.IsActive)
                    {
                        sessions.Add(session);
                    }
                }
            }

            // Sort by last activity descending
            return sessions.OrderByDescending(s => s.LastActivity).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user sessions for user {UserId}", userId);
            return new List<ConversationSession>();
        }
    }

    public async Task<bool> UpdateContextAsync(Guid sessionId, ConversationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var contextKey = GetContextKey(sessionId);
            var contextJson = JsonSerializer.Serialize(context, _jsonOptions);
            
            await _redis.StringSetAsync(contextKey, contextJson, _contextCacheExpiry);
            await UpdateActivityAsync(sessionId, cancellationToken);

            _logger.LogDebug("Updated context for session {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update context for session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> AddMessageAsync(Guid sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Set session ID on message
            message.SessionId = sessionId;

            // Add to messages cache
            var messagesKey = GetMessagesKey(sessionId);
            var messageJson = JsonSerializer.Serialize(message, _jsonOptions);
            await _redis.ListRightPushAsync(messagesKey, messageJson);
            await _redis.KeyExpireAsync(messagesKey, _messagesCacheExpiry);

            // Update session metadata
            var sessionKey = GetSessionKey(sessionId);
            var sessionJson = await _redis.StringGetAsync(sessionKey);
            
            if (sessionJson.HasValue)
            {
                var session = JsonSerializer.Deserialize<ConversationSession>(sessionJson!, _jsonOptions);
                if (session != null)
                {
                    session.MessageCount++;
                    if (message.TokenCount.HasValue)
                    {
                        session.TotalTokensUsed += message.TokenCount.Value;
                    }
                    
                    var updatedSessionJson = JsonSerializer.Serialize(session, _jsonOptions);
                    await _redis.StringSetAsync(sessionKey, updatedSessionJson, _sessionCacheExpiry);
                }
            }

            await UpdateActivityAsync(sessionId, cancellationToken);

            _logger.LogDebug("Added message to session {SessionId} from {Role}", sessionId, message.Role);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add message to session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<List<ChatMessage>> GetOptimizedContextAsync(Guid sessionId, int maxTokens, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await GetSessionMessagesAsync(sessionId);
            if (!messages.Any())
            {
                return new List<ChatMessage>();
            }

            // Calculate token counts if not already present
            foreach (var message in messages.Where(m => !m.TokenCount.HasValue))
            {
                message.TokenCount = _aiAssistantService.EstimateTokenCount(new List<ChatMessage> { message });
            }

            var optimizedMessages = new List<ChatMessage>();
            var currentTokens = 0;

            // Always include system message first if present
            var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
            if (systemMessage != null)
            {
                optimizedMessages.Add(systemMessage);
                currentTokens += systemMessage.TokenCount ?? 0;
            }

            // Add messages from most recent, staying within token limit
            var userAndAssistantMessages = messages
                .Where(m => m.Role != "system")
                .Reverse()
                .ToList();

            foreach (var message in userAndAssistantMessages)
            {
                var messageTokens = message.TokenCount ?? 0;
                if (currentTokens + messageTokens <= maxTokens)
                {
                    optimizedMessages.Insert(systemMessage != null ? 1 : 0, message);
                    currentTokens += messageTokens;
                }
                else
                {
                    break;
                }
            }

            // Ensure proper chronological order
            return optimizedMessages.OrderBy(m => m.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimized context for session {SessionId}", sessionId);
            return new List<ChatMessage>();
        }
    }

    public async Task<bool> ClearSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get session to find user ID
            var session = await GetSessionAsync(sessionId, cancellationToken);
            
            // Remove from Redis
            var sessionKey = GetSessionKey(sessionId);
            var messagesKey = GetMessagesKey(sessionId);
            var contextKey = GetContextKey(sessionId);
            
            await _redis.KeyDeleteAsync(sessionKey);
            await _redis.KeyDeleteAsync(messagesKey);
            await _redis.KeyDeleteAsync(contextKey);

            // Remove from user's session list
            if (session != null)
            {
                var userSessionsKey = GetUserSessionsKey(session.UserId);
                await _redis.SetRemoveAsync(userSessionsKey, sessionId.ToString());
            }

            // Remove from database if persisted
            await DeleteSessionFromDbAsync(sessionId);

            _logger.LogInformation("Cleared conversation session {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionKey = GetSessionKey(sessionId);
            var sessionJson = await _redis.StringGetAsync(sessionKey);
            
            if (sessionJson.HasValue)
            {
                var session = JsonSerializer.Deserialize<ConversationSession>(sessionJson!, _jsonOptions);
                if (session != null)
                {
                    session.LastActivity = DateTime.UtcNow;
                    var updatedJson = JsonSerializer.Serialize(session, _jsonOptions);
                    await _redis.StringSetAsync(sessionKey, updatedJson, _sessionCacheExpiry);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update activity for session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var cleanedCount = 0;
        
        try
        {
            // This is a simple implementation - in production, you might want to use a more efficient approach
            var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
            var sessionKeys = server.Keys(pattern: $"{SESSION_KEY_PREFIX}*");

            foreach (var key in sessionKeys)
            {
                var sessionJson = await _redis.StringGetAsync(key);
                if (sessionJson.HasValue)
                {
                    var session = JsonSerializer.Deserialize<ConversationSession>(sessionJson!, _jsonOptions);
                    if (session != null && session.ExpiresAt < DateTime.UtcNow)
                    {
                        await ClearSessionAsync(session.Id, cancellationToken);
                        cleanedCount++;
                    }
                }
            }

            _logger.LogInformation("Cleaned up {Count} expired conversation sessions", cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired sessions");
        }

        return cleanedCount;
    }

    public async Task<SessionStatistics?> GetSessionStatisticsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            if (session == null)
                return null;

            var messages = await GetSessionMessagesAsync(sessionId);
            
            var stats = new SessionStatistics
            {
                Duration = DateTime.UtcNow - session.CreatedAt,
                MessageCount = messages.Count,
                TokensUsed = session.TotalTokensUsed,
                ErrorCount = messages.Count(m => m.Status == MessageStatus.Failed),
                GoalAchieved = session.Context.GoalProgress >= 1.0
            };

            if (messages.Any())
            {
                var processingTimes = messages
                    .Where(m => m.Metadata?.ProcessingTimeMs.HasValue == true)
                    .Select(m => m.Metadata!.ProcessingTimeMs!.Value)
                    .ToList();

                if (processingTimes.Any())
                {
                    stats.AverageResponseTime = processingTimes.Average();
                }

                // Find most discussed topic (simplified)
                var topics = messages
                    .SelectMany(m => m.Content?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                    .Where(word => word.Length > 4)
                    .GroupBy(word => word.ToLowerInvariant())
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                stats.MostDiscussedTopic = topics?.Key ?? "General Discussion";
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<string?> GenerateSessionSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await GetSessionMessagesAsync(sessionId);
            if (!messages.Any())
                return "No conversation to summarize.";

            // Use AI to generate summary
            return await _aiAssistantService.GenerateConversationTitleAsync(messages, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> ArchiveSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            if (session == null)
                return false;

            session.IsActive = false;
            
            // Update in cache
            var sessionKey = GetSessionKey(sessionId);
            var sessionJson = JsonSerializer.Serialize(session, _jsonOptions);
            await _redis.StringSetAsync(sessionKey, sessionJson, _sessionCacheExpiry);

            // Persist to database
            await PersistSessionToDbAsync(session);

            _logger.LogInformation("Archived conversation session {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive session {SessionId}", sessionId);
            return false;
        }
    }

    // Helper methods
    private static string GetSessionKey(Guid sessionId) => $"{SESSION_KEY_PREFIX}{sessionId}";
    private static string GetUserSessionsKey(Guid userId) => $"{USER_SESSIONS_KEY_PREFIX}{userId}";
    private static string GetMessagesKey(Guid sessionId) => $"{SESSION_MESSAGES_KEY_PREFIX}{sessionId}";
    private static string GetContextKey(Guid sessionId) => $"{SESSION_CONTEXT_KEY_PREFIX}{sessionId}";

    private async Task<List<ChatMessage>> GetSessionMessagesAsync(Guid sessionId)
    {
        var messagesKey = GetMessagesKey(sessionId);
        var messageJsons = await _redis.ListRangeAsync(messagesKey);
        
        var messages = new List<ChatMessage>();
        foreach (var messageJson in messageJsons)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(messageJson!, _jsonOptions);
                if (message != null)
                    messages.Add(message);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize message for session {SessionId}", sessionId);
            }
        }

        return messages.OrderBy(m => m.Timestamp).ToList();
    }

    private async Task<ConversationContext> GetSessionContextAsync(Guid sessionId)
    {
        var contextKey = GetContextKey(sessionId);
        var contextJson = await _redis.StringGetAsync(contextKey);
        
        if (contextJson.HasValue)
        {
            try
            {
                return JsonSerializer.Deserialize<ConversationContext>(contextJson!, _jsonOptions) 
                    ?? new ConversationContext();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize context for session {SessionId}", sessionId);
            }
        }

        return new ConversationContext();
    }

    private async Task PersistSessionToDbAsync(ConversationSession session)
    {
        // TODO: Implement database persistence when conversation history entities are created
        // For now, this is a placeholder that demonstrates the pattern
        await Task.CompletedTask;
    }

    private async Task<ConversationSession?> GetSessionFromDbAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // TODO: Implement database retrieval when conversation history entities are created
        // For now, this is a placeholder that demonstrates the pattern
        await Task.CompletedTask;
        return null;
    }

    private async Task DeleteSessionFromDbAsync(Guid sessionId)
    {
        // TODO: Implement database deletion when conversation history entities are created
        // For now, this is a placeholder that demonstrates the pattern
        await Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for string manipulation
/// </summary>
public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
    }
}