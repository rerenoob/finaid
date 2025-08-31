using finaid.Configuration;
using finaid.Models.AI;
using finaid.Services.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace finaid.Tests.Unit.Services;

public class AzureOpenAIServiceTests
{
    private readonly Mock<ILogger<AzureOpenAIService>> _loggerMock;
    private readonly AzureOpenAISettings _settings;

    public AzureOpenAIServiceTests()
    {
        _loggerMock = new Mock<ILogger<AzureOpenAIService>>();
        _settings = new AzureOpenAISettings
        {
            Endpoint = "https://test-openai.openai.azure.com",
            ApiKey = "test-api-key",
            DeploymentName = "test-deployment",
            ModelName = "gpt-4",
            MaxTokens = 1000,
            Temperature = 0.3,
            MaxContextMessages = 10
        };
    }

    [Fact]
    public void Constructor_WithValidSettings_ShouldInitialize()
    {
        // Arrange
        var options = Options.Create(_settings);

        // Act & Assert - Should not throw
        var service = new AzureOpenAIService(options, _loggerMock.Object);
        service.Dispose();
    }

    [Fact]
    public void Constructor_WithNullSettings_ShouldThrow()
    {
        // Arrange
        IOptions<AzureOpenAISettings> nullOptions = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AzureOpenAIService(nullOptions, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithEmptyEndpoint_ShouldThrow()
    {
        // Arrange
        _settings.Endpoint = "";
        var options = Options.Create(_settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new AzureOpenAIService(options, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ShouldThrow()
    {
        // Arrange
        _settings.ApiKey = "";
        var options = Options.Create(_settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new AzureOpenAIService(options, _loggerMock.Object));
    }

    [Fact]
    public void EstimateTokenCount_WithEmptyMessages_ShouldReturnZero()
    {
        // Arrange
        var options = Options.Create(_settings);
        using var service = new AzureOpenAIService(options, _loggerMock.Object);
        var messages = new List<ChatMessage>();

        // Act
        var tokenCount = service.EstimateTokenCount(messages);

        // Assert
        Assert.Equal(0, tokenCount);
    }

    [Fact]
    public void EstimateTokenCount_WithMessages_ShouldReturnEstimate()
    {
        // Arrange
        var options = Options.Create(_settings);
        using var service = new AzureOpenAIService(options, _loggerMock.Object);
        var messages = new List<ChatMessage>
        {
            ChatMessageFactory.CreateUserMessage("Hello, how can you help me with FAFSA?"),
            ChatMessageFactory.CreateAssistantMessage("I can help you complete your FAFSA form and answer questions about financial aid.")
        };

        // Act
        var tokenCount = service.EstimateTokenCount(messages);

        // Assert
        Assert.True(tokenCount > 0);
        Assert.True(tokenCount < 1000); // Should be reasonable estimate
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ExplainFinancialAidTermAsync_WithInvalidTerm_ShouldReturnErrorMessage(string term)
    {
        // Arrange
        var options = Options.Create(_settings);
        using var service = new AzureOpenAIService(options, _loggerMock.Object);

        // Act
        var result = await service.ExplainFinancialAidTermAsync(term);

        // Assert
        Assert.Equal("Please provide a term to explain.", result);
    }

    [Fact]
    public async Task ValidateFormDataAsync_WithNullFormData_ShouldReturnValidationMessage()
    {
        // Arrange
        var options = Options.Create(_settings);
        using var service = new AzureOpenAIService(options, _loggerMock.Object);

        // Act
        var result = await service.ValidateFormDataAsync(null);

        // Assert
        Assert.Single(result);
        Assert.Equal("No form data provided for validation.", result[0]);
    }

    [Fact]
    public async Task GetChatCompletionAsync_WithEmptyMessages_ShouldReturnError()
    {
        // Arrange
        var options = Options.Create(_settings);
        using var service = new AzureOpenAIService(options, _loggerMock.Object);
        var messages = new List<ChatMessage>();

        // Act
        var result = await service.GetChatCompletionAsync(messages);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No messages provided", result.ErrorMessage);
        Assert.Equal(AIErrorCode.InvalidRequest, result.ErrorCode);
    }

    [Fact]
    public async Task GetFormAssistanceAsync_WithEmptyInput_ShouldReturnError()
    {
        // Arrange
        var options = Options.Create(_settings);
        using var service = new AzureOpenAIService(options, _loggerMock.Object);
        var formContext = new { section = "studentInfo" };

        // Act
        var result = await service.GetFormAssistanceAsync("", formContext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User input is required", result.ErrorMessage);
        Assert.Equal(AIErrorCode.InvalidRequest, result.ErrorCode);
    }

    [Fact]
    public async Task GenerateConversationTitleAsync_WithEmptyMessages_ShouldReturnDefaultTitle()
    {
        // Arrange
        var options = Options.Create(_settings);
        using var service = new AzureOpenAIService(options, _loggerMock.Object);
        var messages = new List<ChatMessage>();

        // Act
        var result = await service.GenerateConversationTitleAsync(messages);

        // Assert
        Assert.Equal("New Conversation", result);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_settings);
        var service = new AzureOpenAIService(options, _loggerMock.Object);

        // Act & Assert - Should not throw
        service.Dispose();
        service.Dispose(); // Should handle multiple dispose calls
    }
}