using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using finaid.Configuration;
using finaid.Models.AI;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace finaid.Services.AI;

/// <summary>
/// AWS Bedrock service implementation for AI assistant functionality
/// </summary>
public class AWSBedrockService : IAIAssistantService, IDisposable
{
    private readonly AmazonBedrockRuntimeClient _bedrockClient;
    private readonly AWSBedrockSettings _settings;
    private readonly ILogger<AWSBedrockService> _logger;
    private bool _disposed = false;

    public AWSBedrockService(
        IOptions<AWSBedrockSettings> settings,
        ILogger<AWSBedrockService> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ValidateSettings();
        _bedrockClient = CreateBedrockClient();

        _logger.LogInformation("AWS Bedrock service initialized with model: {ModelId}", _settings.ModelId);
    }

    public async Task<AIResponse> GetChatCompletionAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null || !messages.Any())
        {
            return AIResponse.Error("No messages provided", AIErrorCode.InvalidRequest);
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();

            _logger.LogDebug("Starting chat completion request {RequestId} with {MessageCount} messages", 
                requestId, messages.Count);

            var optimizedMessages = OptimizeMessagesForContext(messages);
            var request = CreateChatRequest(optimizedMessages);

            var response = await ExecuteWithRetryAsync(async () =>
            {
                return await _bedrockClient.InvokeModelAsync(request, cancellationToken);
            });

            stopwatch.Stop();

            if (response?.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = ExtractContentFromResponse(response);
                var metadata = CreateResponseMetadata(response, stopwatch.Elapsed, requestId);

                _logger.LogInformation("Chat completion successful for request {RequestId}. Time: {Duration}ms",
                    requestId, stopwatch.ElapsedMilliseconds);

                return AIResponse.Success(content, metadata);
            }

            _logger.LogWarning("No valid response received for request {RequestId}", requestId);
            return AIResponse.Error("No response generated", AIErrorCode.Unknown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat completion: {ErrorMessage}", ex.Message);
            return MapExceptionToAIResponse(ex);
        }
    }

    public async Task<AIResponse> GetFormAssistanceAsync(string userInput, object formContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return AIResponse.Error("User input is required", AIErrorCode.InvalidRequest);
        }

        try
        {
            var formContextJson = JsonSerializer.Serialize(formContext, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var systemMessage = ChatMessageFactory.CreateSystemMessage($"""
                {_settings.SystemMessage}
                
                FORM CONTEXT:
                The user is currently working on a FAFSA form with the following context:
                {formContextJson}
                
                Please provide specific, actionable guidance related to their question and current form state.
                Focus on helping them complete the form accurately and efficiently.
                """);

            var userMessage = ChatMessageFactory.CreateUserMessage(userInput);

            var messages = new List<ChatMessage> { systemMessage, userMessage };
            return await GetChatCompletionAsync(messages, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in form assistance: {ErrorMessage}", ex.Message);
            return AIResponse.Error($"Failed to provide form assistance: {ex.Message}", AIErrorCode.Unknown);
        }
    }

    public async Task<string> ExplainFinancialAidTermAsync(string term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return "Please provide a term to explain.";
        }

        try
        {
            var systemMessage = ChatMessageFactory.CreateSystemMessage($"""
                {_settings.SystemMessage}
                
                Your task is to explain financial aid terms in simple, clear language that students and parents can understand.
                Provide a concise explanation (2-3 sentences) followed by a practical example if helpful.
                """);

            var userMessage = ChatMessageFactory.CreateUserMessage($"Please explain this financial aid term: {term}");

            var messages = new List<ChatMessage> { systemMessage, userMessage };
            var response = await GetChatCompletionAsync(messages, cancellationToken);

            return response.IsSuccess ? response.Content : $"I couldn't explain the term '{term}' at this time. Please try again later.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining term '{Term}': {ErrorMessage}", term, ex.Message);
            return $"I encountered an error while explaining '{term}'. Please try again later.";
        }
    }

    public async Task<List<string>> ValidateFormDataAsync(object formData, CancellationToken cancellationToken = default)
    {
        var suggestions = new List<string>();

        if (formData == null)
        {
            suggestions.Add("No form data provided for validation.");
            return suggestions;
        }

        try
        {
            var formDataJson = JsonSerializer.Serialize(formData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var systemMessage = ChatMessageFactory.CreateSystemMessage($"""
                {_settings.SystemMessage}
                
                Your task is to validate FAFSA form data and provide specific suggestions for improvement.
                Analyze the provided form data and return a JSON array of validation suggestions.
                Each suggestion should be a clear, actionable instruction.
                Focus on:
                - Missing required fields
                - Inconsistent information
                - Common mistakes
                - Optimization opportunities
                
                Return only a JSON array of strings, no additional text.
                """);

            var userMessage = ChatMessageFactory.CreateUserMessage($"Please validate this FAFSA form data:\n{formDataJson}");

            var messages = new List<ChatMessage> { systemMessage, userMessage };
            var response = await GetChatCompletionAsync(messages, cancellationToken);

            if (response.IsSuccess)
            {
                try
                {
                    // Extract JSON array from response
                    var jsonMatch = Regex.Match(response.Content, @"\[(.*?)\]", RegexOptions.Singleline);
                    if (jsonMatch.Success)
                    {
                        var jsonArray = JsonSerializer.Deserialize<string[]>($"[{jsonMatch.Groups[1].Value}]");
                        suggestions.AddRange(jsonArray ?? Array.Empty<string>());
                    }
                    else
                    {
                        // Fallback: treat entire response as a single suggestion
                        suggestions.Add(response.Content);
                    }
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, treat response as plain text
                    suggestions.Add(response.Content);
                }
            }
            else
            {
                suggestions.Add("Unable to validate form data at this time.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating form data: {ErrorMessage}", ex.Message);
            suggestions.Add($"Validation error: {ex.Message}");
        }

        return suggestions.Any() ? suggestions : new List<string> { "Form data appears to be valid." };
    }

    public async IAsyncEnumerable<string> GetStreamingChatCompletionAsync(
        List<ChatMessage> messages, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (messages == null || !messages.Any())
        {
            yield return "Error: No messages provided";
            yield break;
        }

        if (!_settings.EnableStreaming)
        {
            var response = await GetChatCompletionAsync(messages, cancellationToken);
            yield return response.Content;
            yield break;
        }

        // Note: AWS Bedrock streaming is more complex and requires different API calls
        // For now, we'll use non-streaming and yield the complete response
        var streamingResponse = await GetChatCompletionAsync(messages, cancellationToken);
        yield return streamingResponse.Content;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new InvokeModelRequest
            {
                ModelId = _settings.ModelId,
                ContentType = "application/json",
                Body = GenerateJsonBody(new List<ChatMessage> 
                { 
                    ChatMessageFactory.CreateUserMessage("Say 'OK'") 
                })
            };

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _bedrockClient.InvokeModelAsync(request, timeoutCts.Token);
            return response?.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public int EstimateTokenCount(List<ChatMessage> messages)
    {
        if (messages == null || !messages.Any())
            return 0;

        // Rough estimation: ~1 token per 4 characters for English text
        var totalChars = messages.Sum(m => (m.Content?.Length ?? 0) + m.Role.Length + 10); // +10 for formatting
        return (int)Math.Ceiling(totalChars / 4.0);
    }

    public async Task<string> GenerateConversationTitleAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null || !messages.Any())
        {
            return "New Conversation";
        }

        try
        {
            var recentMessages = messages.Where(m => m.Role != "system").Take(5).ToList();
            var conversationContext = string.Join("\n", recentMessages.Select(m => $"{m.Role}: {m.Content}"));

            var systemMessage = ChatMessageFactory.CreateSystemMessage(
                "Generate a short, descriptive title (4-6 words) for this conversation. " +
                "Focus on the main topic or question being discussed. Return only the title, no quotes or additional text.");

            var userMessage = ChatMessageFactory.CreateUserMessage($"Generate a title for this conversation:\n{conversationContext}");

            var titleMessages = new List<ChatMessage> { systemMessage, userMessage };
            var response = await GetChatCompletionAsync(titleMessages, cancellationToken);

            if (response.IsSuccess && !string.IsNullOrWhiteSpace(response.Content))
            {
                return response.Content.Trim().Trim('"');
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate conversation title: {ErrorMessage}", ex.Message);
        }

        return "Financial Aid Discussion";
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Region))
            throw new InvalidOperationException("AWS Region is not configured");

        if (string.IsNullOrWhiteSpace(_settings.AccessKeyId))
            throw new InvalidOperationException("AWS Access Key ID is not configured");

        if (string.IsNullOrWhiteSpace(_settings.SecretAccessKey))
            throw new InvalidOperationException("AWS Secret Access Key is not configured");

        if (string.IsNullOrWhiteSpace(_settings.ModelId))
            throw new InvalidOperationException("AWS Bedrock Model ID is not configured");
    }

    private AmazonBedrockRuntimeClient CreateBedrockClient()
    {
        var config = new AmazonBedrockRuntimeConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_settings.Region),
            Timeout = TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds)
        };

        return new AmazonBedrockRuntimeClient(_settings.AccessKeyId, _settings.SecretAccessKey, config);
    }

    private InvokeModelRequest CreateChatRequest(List<ChatMessage> messages)
    {
        var request = new InvokeModelRequest
        {
            ModelId = _settings.ModelId,
            ContentType = "application/json",
            Body = GenerateJsonBody(messages)
        };

        return request;
    }

    private MemoryStream GenerateJsonBody(List<ChatMessage> messages)
    {
        var claudeMessages = messages.Select(m => new
        {
            role = m.Role,
            content = new[] { new { type = "text", text = m.Content } }
        }).ToList();

        var requestBody = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = _settings.MaxTokens,
            temperature = _settings.Temperature,
            top_p = _settings.TopP,
            messages = claudeMessages
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
    }

    private string ExtractContentFromResponse(InvokeModelResponse response)
    {
        using var reader = new StreamReader(response.Body);
        var json = reader.ReadToEnd();
        
        var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("content", out var contentArray))
        {
            foreach (var contentItem in contentArray.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        var retryCount = 0;
        var delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMs);

        while (retryCount < _settings.MaxRetryAttempts)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (retryCount < _settings.MaxRetryAttempts - 1 && IsRetryableException(ex))
            {
                retryCount++;
                _logger.LogWarning("Retry attempt {Attempt} for AWS Bedrock request: {Exception}",
                    retryCount, ex.Message);
                
                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }

        // If we get here, all retries failed - let the exception propagate
        return await operation();
    }

    private static bool IsRetryableException(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex.Message.Contains("Throttling") ||
               ex.Message.Contains("ServiceUnavailable");
    }

    private List<ChatMessage> OptimizeMessagesForContext(List<ChatMessage> messages)
    {
        if (messages.Count <= _settings.MaxContextMessages)
        {
            return messages;
        }

        var optimizedMessages = new List<ChatMessage>();
        
        // Always keep the system message first if present
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        if (systemMessage != null)
        {
            optimizedMessages.Add(systemMessage);
        }

        // Take the most recent messages up to the limit
        var recentMessages = messages
            .Where(m => m.Role != "system")
            .TakeLast(_settings.MaxContextMessages - (systemMessage != null ? 1 : 0))
            .ToList();

        optimizedMessages.AddRange(recentMessages);

        _logger.LogDebug("Optimized context from {OriginalCount} to {OptimizedCount} messages",
            messages.Count, optimizedMessages.Count);

        return optimizedMessages;
    }

    private AIResponseMetadata CreateResponseMetadata(InvokeModelResponse response, TimeSpan processingTime, string requestId)
    {
        return new AIResponseMetadata
        {
            Model = _settings.ModelId,
            ProcessingTime = processingTime,
            RequestId = requestId,
            RequestTimestamp = DateTime.UtcNow.Subtract(processingTime),
            ResponseTimestamp = DateTime.UtcNow
        };
    }

    private static AIResponse MapExceptionToAIResponse(Exception ex)
    {
        return ex switch
        {
            TaskCanceledException or OperationCanceledException => 
                AIResponse.Error("Request was cancelled or timed out", AIErrorCode.Timeout),
            ArgumentException => 
                AIResponse.Error($"Invalid request: {ex.Message}", AIErrorCode.InvalidRequest),
            Amazon.BedrockRuntime.Model.ThrottlingException => 
                AIResponse.Error("Rate limit exceeded", AIErrorCode.RateLimitExceeded),
            Amazon.BedrockRuntime.Model.ModelTimeoutException => 
                AIResponse.Error("Service temporarily unavailable", AIErrorCode.ServiceUnavailable),
            _ => 
                AIResponse.Error($"Unexpected error: {ex.Message}", AIErrorCode.Unknown)
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _bedrockClient?.Dispose();
            _disposed = true;
        }
    }
}