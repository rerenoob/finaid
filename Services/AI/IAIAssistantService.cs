using finaid.Models.AI;

namespace finaid.Services.AI;

/// <summary>
/// Interface for AI assistant services that provide conversational AI capabilities
/// </summary>
public interface IAIAssistantService
{
    /// <summary>
    /// Get a chat completion response from the AI model
    /// </summary>
    /// <param name="messages">List of conversation messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with content and metadata</returns>
    Task<AIResponse> GetChatCompletionAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get form assistance based on user input and current form context
    /// </summary>
    /// <param name="userInput">User's question or input</param>
    /// <param name="formContext">Current form data and context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with form-specific guidance</returns>
    Task<AIResponse> GetFormAssistanceAsync(string userInput, object formContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explain a financial aid term in simple language
    /// </summary>
    /// <param name="term">Financial aid term to explain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plain language explanation of the term</returns>
    Task<string> ExplainFinancialAidTermAsync(string term, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate form data and provide suggestions for improvement
    /// </summary>
    /// <param name="formData">Form data to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of validation suggestions or issues found</returns>
    Task<List<string>> ValidateFormDataAsync(object formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a streaming response for real-time chat
    /// </summary>
    /// <param name="messages">List of conversation messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response chunks</returns>
    IAsyncEnumerable<string> GetStreamingChatCompletionAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the AI service is healthy and responsive
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is healthy</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get estimated token count for a list of messages
    /// </summary>
    /// <param name="messages">Messages to analyze</param>
    /// <returns>Estimated token count</returns>
    int EstimateTokenCount(List<ChatMessage> messages);

    /// <summary>
    /// Generate a title for a conversation based on the messages
    /// </summary>
    /// <param name="messages">Conversation messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated conversation title</returns>
    Task<string> GenerateConversationTitleAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default);
}