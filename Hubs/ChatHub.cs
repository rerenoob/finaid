using Microsoft.AspNetCore.SignalR;
using finaid.Services.AI;
using finaid.Models.AI;
using System.Text.Json;

namespace finaid.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication
/// </summary>
public class ChatHub : Hub
{
    private readonly IAIAssistantService _aiService;
    private readonly IConversationContextService _contextService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IAIAssistantService aiService,
        IConversationContextService contextService,
        ILogger<ChatHub> logger)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task JoinSession(string sessionId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            _logger.LogDebug("User {ConnectionId} joined chat session {SessionId}", Context.ConnectionId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining session {SessionId} for connection {ConnectionId}", sessionId, Context.ConnectionId);
        }
    }

    public async Task LeaveSession(string sessionId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
            _logger.LogDebug("User {ConnectionId} left chat session {SessionId}", Context.ConnectionId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving session {SessionId} for connection {ConnectionId}", sessionId, Context.ConnectionId);
        }
    }

    public async Task SendMessage(string sessionId, string message, string? formContext = null)
    {
        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid session ID");
                return;
            }

            // Notify group that user is typing
            await Clients.Group(sessionId).SendAsync("UserTyping", Context.ConnectionId, false);

            // Create user message
            var userMessage = ChatMessageFactory.CreateUserMessage(message, sessionId: sessionGuid);
            userMessage.Metadata = new MessageMetadata
            {
                Source = "chat_interface",
                Context = new Dictionary<string, object>
                {
                    ["connectionId"] = Context.ConnectionId,
                    ["timestamp"] = DateTime.UtcNow
                }
            };

            // Add message to conversation
            await _contextService.AddMessageAsync(sessionGuid, userMessage);

            // Send user message to all clients in the group
            await Clients.Group(sessionId).SendAsync("MessageReceived", userMessage);

            // Notify group that AI is thinking
            await Clients.Group(sessionId).SendAsync("AITyping", true);

            try
            {
                // Get AI response
                AIResponse aiResponse;
                if (!string.IsNullOrWhiteSpace(formContext))
                {
                    var contextObject = JsonSerializer.Deserialize<object>(formContext);
                    aiResponse = await _aiService.GetFormAssistanceAsync(message, contextObject);
                }
                else
                {
                    var conversationMessages = await _contextService.GetOptimizedContextAsync(sessionGuid, 8000);
                    aiResponse = await _aiService.GetChatCompletionAsync(conversationMessages);
                }

                // Create AI message
                var aiMessage = ChatMessageFactory.CreateAssistantMessage(
                    aiResponse.Content, 
                    sessionGuid, 
                    new MessageMetadata
                    {
                        Source = "ai_assistant",
                        ConfidenceLevel = aiResponse.ConfidenceLevel,
                        ProcessingTimeMs = (int)aiResponse.Metadata.ProcessingTime.TotalMilliseconds,
                        ModelVersion = aiResponse.Metadata.Model,
                        Context = new Dictionary<string, object>
                        {
                            ["tokenUsage"] = new 
                            { 
                                prompt = aiResponse.Metadata.PromptTokens,
                                completion = aiResponse.Metadata.CompletionTokens,
                                total = aiResponse.Metadata.TotalTokens
                            },
                            ["finishReason"] = aiResponse.Metadata.FinishReason ?? "completed",
                            ["requestId"] = aiResponse.Metadata.RequestId ?? Guid.NewGuid().ToString()
                        }
                    });

                if (!aiResponse.IsSuccess)
                {
                    aiMessage.Status = MessageStatus.Failed;
                    aiMessage.ErrorMessage = aiResponse.ErrorMessage;
                }

                // Add AI message to conversation
                await _contextService.AddMessageAsync(sessionGuid, aiMessage);

                // Stop AI typing indicator
                await Clients.Group(sessionId).SendAsync("AITyping", false);

                // Send AI response to all clients in the group
                await Clients.Group(sessionId).SendAsync("MessageReceived", aiMessage);

                // Send suggested actions if any
                if (aiResponse.SuggestedActions.Any())
                {
                    await Clients.Group(sessionId).SendAsync("SuggestedActions", aiResponse.SuggestedActions);
                }
            }
            catch (Exception aiEx)
            {
                _logger.LogError(aiEx, "Error getting AI response for session {SessionId}", sessionId);
                
                // Stop AI typing indicator
                await Clients.Group(sessionId).SendAsync("AITyping", false);

                // Send error message
                var errorMessage = ChatMessageFactory.CreateErrorMessage(
                    "I'm sorry, I encountered an error while processing your message. Please try again.",
                    message,
                    sessionGuid);
                
                await _contextService.AddMessageAsync(sessionGuid, errorMessage);
                await Clients.Group(sessionId).SendAsync("MessageReceived", errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message in session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", "Failed to process message. Please try again.");
        }
    }

    public async Task SendStreamingMessage(string sessionId, string message, string? formContext = null)
    {
        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid session ID");
                return;
            }

            // Create user message
            var userMessage = ChatMessageFactory.CreateUserMessage(message, sessionId: sessionGuid);
            await _contextService.AddMessageAsync(sessionGuid, userMessage);
            await Clients.Group(sessionId).SendAsync("MessageReceived", userMessage);

            // Notify group that AI is streaming
            await Clients.Group(sessionId).SendAsync("AIStreamingStart");

            try
            {
                var conversationMessages = await _contextService.GetOptimizedContextAsync(sessionGuid, 8000);
                var streamingId = Guid.NewGuid().ToString();

                // Start streaming response
                await foreach (var chunk in _aiService.GetStreamingChatCompletionAsync(conversationMessages))
                {
                    await Clients.Group(sessionId).SendAsync("StreamingChunk", streamingId, chunk);
                }

                // Indicate streaming is complete
                await Clients.Group(sessionId).SendAsync("AIStreamingComplete", streamingId);
            }
            catch (Exception aiEx)
            {
                _logger.LogError(aiEx, "Error getting streaming AI response for session {SessionId}", sessionId);
                await Clients.Group(sessionId).SendAsync("AIStreamingError", "Error generating response");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming message in session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", "Failed to process streaming message. Please try again.");
        }
    }

    public async Task NotifyTyping(string sessionId, bool isTyping)
    {
        try
        {
            await Clients.OthersInGroup(sessionId).SendAsync("UserTyping", Context.ConnectionId, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying typing status for session {SessionId}", sessionId);
        }
    }

    public async Task RequestSessionHistory(string sessionId)
    {
        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid session ID");
                return;
            }

            var session = await _contextService.GetSessionAsync(sessionGuid);
            if (session != null)
            {
                await Clients.Caller.SendAsync("SessionHistory", session.Messages);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Session not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session history for {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", "Failed to retrieve session history");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("User {ConnectionId} disconnected. Exception: {Exception}", 
            Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("User {ConnectionId} connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}