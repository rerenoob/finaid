using finaid.Models.AI;
using System.Text.Json;

namespace finaid.Services.AI;

/// <summary>
/// Mock implementation of AI assistant service for development/testing purposes
/// </summary>
public class MockAIAssistantService : IAIAssistantService
{
    private readonly ILogger<MockAIAssistantService> _logger;
    private static readonly Random _random = new();

    public MockAIAssistantService(ILogger<MockAIAssistantService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("Mock AI Assistant service initialized for development");
    }

    public async Task<AIResponse> GetChatCompletionAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null || !messages.Any())
        {
            return AIResponse.Error("No messages provided", AIErrorCode.InvalidRequest);
        }

        try
        {
            _logger.LogDebug("Mock AI service processing {MessageCount} messages", messages.Count);

            // Simulate processing time
            await Task.Delay(_random.Next(500, 1500), cancellationToken);

            var lastMessage = messages.LastOrDefault()?.Content ?? "";
            var mockResponse = GenerateMockResponse(lastMessage);

            return AIResponse.Success(mockResponse, new AIResponseMetadata
            {
                PromptTokens = _random.Next(50, 200),
                CompletionTokens = _random.Next(100, 300),
                TotalTokens = _random.Next(150, 500),
                ProcessingTime = TimeSpan.FromMilliseconds(_random.Next(500, 1500)),
                Model = "mock-gpt-4"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Mock AI request cancelled");
            return AIResponse.Error("Request cancelled", AIErrorCode.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mock AI service");
            return AIResponse.Error($"Mock service error: {ex.Message}", AIErrorCode.Unknown);
        }
    }

    public async Task<AIResponse> GenerateFormAssistanceAsync(string formType, object formData, string userQuestion = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mock AI generating form assistance for {FormType}", formType);

            // Simulate processing time
            await Task.Delay(_random.Next(800, 2000), cancellationToken);

            var mockAssistance = GenerateMockFormAssistance(formType, userQuestion);

            return AIResponse.Success(mockAssistance, new AIResponseMetadata
            {
                PromptTokens = _random.Next(100, 300),
                CompletionTokens = _random.Next(150, 400),
                TotalTokens = _random.Next(250, 700),
                ProcessingTime = TimeSpan.FromMilliseconds(_random.Next(800, 2000)),
                Model = "mock-gpt-4"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Mock AI form assistance request cancelled");
            return AIResponse.Error("Request cancelled", AIErrorCode.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mock form assistance");
            return AIResponse.Error($"Mock service error: {ex.Message}", AIErrorCode.Unknown);
        }
    }

    public async Task<AIResponse> ReviewFormDataAsync(string formType, object formData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mock AI reviewing form data for {FormType}", formType);

            // Simulate processing time
            await Task.Delay(_random.Next(1000, 2500), cancellationToken);

            var mockReview = GenerateMockFormReview(formType);

            return AIResponse.Success(mockReview, new AIResponseMetadata
            {
                PromptTokens = _random.Next(200, 500),
                CompletionTokens = _random.Next(300, 600),
                TotalTokens = _random.Next(500, 1100),
                ProcessingTime = TimeSpan.FromMilliseconds(_random.Next(1000, 2500)),
                Model = "mock-gpt-4"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Mock AI form review request cancelled");
            return AIResponse.Error("Request cancelled", AIErrorCode.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mock form review");
            return AIResponse.Error($"Mock service error: {ex.Message}", AIErrorCode.Unknown);
        }
    }

    public async Task<AIResponse> GetFormAssistanceAsync(string userInput, object formContext, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mock AI providing form assistance for input: {UserInput}", userInput);

            // Simulate processing time
            await Task.Delay(_random.Next(500, 1200), cancellationToken);

            var mockAssistance = GenerateMockFormAssistance("FAFSA", userInput);

            return AIResponse.Success(mockAssistance, new AIResponseMetadata
            {
                PromptTokens = _random.Next(75, 200),
                CompletionTokens = _random.Next(100, 250),
                TotalTokens = _random.Next(175, 450),
                ProcessingTime = TimeSpan.FromMilliseconds(_random.Next(500, 1200)),
                Model = "mock-gpt-4"
            });
        }
        catch (OperationCanceledException)
        {
            return AIResponse.Error("Request cancelled", AIErrorCode.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mock form assistance");
            return AIResponse.Error($"Mock service error: {ex.Message}", AIErrorCode.Unknown);
        }
    }

    public async Task<string> ExplainFinancialAidTermAsync(string term, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mock AI explaining financial aid term: {Term}", term);

            // Simulate processing time
            await Task.Delay(_random.Next(300, 800), cancellationToken);

            return GenerateMockTermExplanation(term);
        }
        catch (OperationCanceledException)
        {
            return "Request was cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining term");
            return $"Sorry, I couldn't explain that term right now: {ex.Message}";
        }
    }

    public async Task<List<string>> ValidateFormDataAsync(object formData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mock AI validating form data");

            // Simulate processing time
            await Task.Delay(_random.Next(400, 1000), cancellationToken);

            return GenerateMockValidation();
        }
        catch (OperationCanceledException)
        {
            return new List<string> { "Validation request was cancelled." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating form data");
            return new List<string> { $"Validation error: {ex.Message}" };
        }
    }

    public async IAsyncEnumerable<string> GetStreamingChatCompletionAsync(List<ChatMessage> messages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = GenerateMockResponse(messages?.LastOrDefault()?.Content ?? "");
        var words = response.Split(' ');

        foreach (var word in words)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return word + " ";
            await Task.Delay(_random.Next(50, 150), cancellationToken);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Mock service is always healthy
            await Task.Delay(10, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public int EstimateTokenCount(List<ChatMessage> messages)
    {
        if (messages == null || !messages.Any())
            return 0;

        // Simple mock estimation: ~4 characters per token
        var totalChars = messages.Sum(m => m.Content?.Length ?? 0);
        return (int)Math.Ceiling(totalChars / 4.0);
    }

    public async Task<string> GenerateConversationTitleAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(_random.Next(200, 500), cancellationToken);

            var titles = new[]
            {
                "FAFSA Help Session",
                "Financial Aid Questions",
                "Form Assistance Chat",
                "Student Aid Discussion",
                "Application Support"
            };

            return titles[_random.Next(titles.Length)];
        }
        catch (OperationCanceledException)
        {
            return "Chat Session";
        }
    }

    private static string GenerateMockResponse(string userMessage)
    {
        var responses = new[]
        {
            "I understand you're looking for assistance with your FAFSA application. Let me help you with that information.",
            "Based on your question, here are some key points to consider for your financial aid application.",
            "That's a great question! Let me provide some guidance on this aspect of the FAFSA process.",
            "I can help clarify that for you. Here's what you need to know about this requirement.",
            "This is an important part of the application process. Let me walk you through the details."
        };

        var baseResponse = responses[_random.Next(responses.Length)];
        
        // Add context-specific information based on keywords
        if (userMessage.ToLowerInvariant().Contains("income"))
        {
            baseResponse += " When reporting income, make sure to use the information from your tax returns or W-2 forms from the appropriate tax year.";
        }
        else if (userMessage.ToLowerInvariant().Contains("dependent"))
        {
            baseResponse += " Your dependency status affects which questions you'll need to answer and whose financial information is required.";
        }
        else if (userMessage.ToLowerInvariant().Contains("school"))
        {
            baseResponse += " Remember to include all schools you're interested in attending, as each school will receive your FAFSA information.";
        }
        else
        {
            baseResponse += " Make sure to have all required documents ready before you begin, including tax returns, bank statements, and Social Security cards.";
        }

        return baseResponse;
    }

    private static string GenerateMockFormAssistance(string formType, string userQuestion)
    {
        var assistanceResponses = new[]
        {
            "Here's some helpful guidance for completing this section of your FAFSA application:",
            "Let me provide some tips to help you with this part of the form:",
            "Based on common questions, here's what you should know:",
            "This section can be confusing, so let me break it down for you:"
        };

        var tips = new[]
        {
            "• Double-check all information for accuracy before submitting",
            "• Have your tax documents ready for reference", 
            "• Consider the impact of your answers on your aid eligibility",
            "• Don't hesitate to contact your school's financial aid office if you need help",
            "• Save your progress frequently as you complete the form"
        };

        var response = assistanceResponses[_random.Next(assistanceResponses.Length)] + "\n\n";
        
        // Add random tips
        var selectedTips = tips.OrderBy(x => _random.Next()).Take(_random.Next(2, 4));
        response += string.Join("\n", selectedTips);
        
        return response;
    }

    private static string GenerateMockFormReview(string formType)
    {
        var reviews = new[]
        {
            "**Form Review Summary**\n\nI've reviewed your FAFSA application and here are my observations:",
            "**Application Analysis**\n\nBased on the information provided, here's what I found:",
            "**FAFSA Review Results**\n\nYour application looks good overall. Here are some key points:"
        };

        var findings = new[]
        {
            "\n\n✅ **Strengths:**\n• All required fields appear to be completed\n• Tax information is consistent with reported income\n• School selections are appropriate",
            "\n\n⚠️ **Recommendations:**\n• Consider adding additional school choices to maximize opportunities\n• Review dependency status answers for accuracy\n• Verify all financial information matches your tax documents",
            "\n\n📋 **Next Steps:**\n• Submit your application before the priority deadline\n• Monitor your Student Aid Report (SAR) for any corrections needed\n• Contact your school's financial aid office with any questions"
        };

        var baseReview = reviews[_random.Next(reviews.Length)];
        var selectedFindings = findings.OrderBy(x => _random.Next()).Take(_random.Next(2, 3));
        
        return baseReview + string.Join("", selectedFindings);
    }

    private static string GenerateMockTermExplanation(string term)
    {
        var explanations = new Dictionary<string, string>
        {
            { "EFC", "Expected Family Contribution (EFC) is the amount your family is expected to contribute toward your college costs. It's calculated using your family's income, assets, and other factors." },
            { "SAI", "Student Aid Index (SAI) replaces EFC and represents your family's financial strength. A lower SAI generally means more financial aid eligibility." },
            { "FAFSA", "Free Application for Federal Student Aid (FAFSA) is the form you fill out to apply for federal student aid, including grants, loans, and work-study programs." },
            { "Pell Grant", "A federal grant that provides need-based aid to undergraduate students. Unlike loans, grants don't need to be repaid." },
            { "Cost of Attendance", "The total amount it will cost to go to school, including tuition, room and board, books, transportation, and personal expenses." },
            { "Dependency Status", "Determines whether you're considered dependent on your parents for financial aid purposes, which affects whose income information is required." }
        };

        if (explanations.ContainsKey(term))
        {
            return explanations[term];
        }

        return $"{term} is an important financial aid concept. It relates to determining your eligibility for various types of student financial assistance programs.";
    }

    private static List<string> GenerateMockValidation()
    {
        var validationMessages = new[]
        {
            "All required fields appear to be completed correctly.",
            "Consider double-checking your Social Security Number format.",
            "Make sure your income information matches your tax documents.",
            "Verify that all school codes are correct and complete.",
            "Review your dependency status answers for accuracy."
        };

        var selectedMessages = validationMessages
            .OrderBy(x => _random.Next())
            .Take(_random.Next(2, 4))
            .ToList();

        return selectedMessages;
    }

    public void Dispose()
    {
        // Mock service doesn't need disposal
        _logger.LogDebug("Mock AI Assistant service disposed");
    }
}