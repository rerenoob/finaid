using finaid.Data;
using finaid.Models.AI;
using finaid.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace finaid.Tests.Unit.Services;

public class ConversationContextServiceTests
{
    private readonly Mock<ApplicationDbContext> _dbContextMock;
    private readonly Mock<IDatabase> _redisMock;
    private readonly Mock<IConnectionMultiplexer> _connectionMock;
    private readonly Mock<IAIAssistantService> _aiServiceMock;
    private readonly Mock<ILogger<ConversationContextService>> _loggerMock;

    public ConversationContextServiceTests()
    {
        _dbContextMock = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
        _redisMock = new Mock<IDatabase>();
        _connectionMock = new Mock<IConnectionMultiplexer>();
        _aiServiceMock = new Mock<IAIAssistantService>();
        _loggerMock = new Mock<ILogger<ConversationContextService>>();

        _connectionMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisMock.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Arrange & Act
        var service = new ConversationContextService(
            _dbContextMock.Object,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullDbContext_ShouldThrow()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConversationContextService(
            null!,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullRedis_ShouldThrow()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConversationContextService(
            _dbContextMock.Object,
            null!,
            _aiServiceMock.Object,
            _loggerMock.Object));
    }

    [Fact]
    public async Task CreateSessionAsync_WithValidParameters_ShouldCreateSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionType = "fafsa";

        _redisMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);
        _redisMock.Setup(x => x.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);
        _redisMock.Setup(x => x.ExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);

        var service = new ConversationContextService(
            _dbContextMock.Object,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.CreateSessionAsync(userId, sessionType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(sessionType, result.SessionType);
        Assert.True(result.IsActive);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task GetSessionAsync_WithNonExistentSession_ShouldReturnNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        
        _redisMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(RedisValue.Null);

        var service = new ConversationContextService(
            _dbContextMock.Object,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GetSessionAsync(sessionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateContextAsync_WithValidSession_ShouldUpdateContext()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var context = new ConversationContext
        {
            CurrentSection = "student-info"
        };

        _redisMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);
        _redisMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(RedisValue.Null); // Session not found for activity update

        var service = new ConversationContextService(
            _dbContextMock.Object,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.UpdateContextAsync(sessionId, context);

        // Assert
        Assert.True(result);
        _redisMock.Verify(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddMessageAsync_WithValidMessage_ShouldAddMessage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var message = ChatMessageFactory.CreateUserMessage("Hello, I need help with FAFSA");

        _redisMock.Setup(x => x.ListRightPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(1);
        _redisMock.Setup(x => x.ExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);
        _redisMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(RedisValue.Null); // Session not found

        var service = new ConversationContextService(
            _dbContextMock.Object,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.AddMessageAsync(sessionId, message);

        // Assert
        Assert.True(result);
        Assert.Equal(sessionId, message.SessionId);
    }

    [Fact]
    public async Task GetOptimizedContextAsync_WithNoMessages_ShouldReturnEmptyList()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var maxTokens = 1000;

        _redisMock.Setup(x => x.ListRangeAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(Array.Empty<RedisValue>());

        var service = new ConversationContextService(
            _dbContextMock.Object,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GetOptimizedContextAsync(sessionId, maxTokens);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ClearSessionAsync_WithValidSession_ShouldClearSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _redisMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(RedisValue.Null);
        _redisMock.Setup(x => x.DeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(3);

        var service = new ConversationContextService(
            _dbContextMock.Object,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.ClearSessionAsync(sessionId);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("fafsa")]
    [InlineData("general")]
    [InlineData("document")]
    public async Task CreateSessionAsync_WithDifferentSessionTypes_ShouldCreateAppropriateSession(string sessionType)
    {
        // Arrange
        var userId = Guid.NewGuid();

        _redisMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);
        _redisMock.Setup(x => x.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);
        _redisMock.Setup(x => x.ExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);

        var service = new ConversationContextService(
            _dbContextMock.Object,
            _connectionMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.CreateSessionAsync(userId, sessionType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionType, result.SessionType);
        Assert.Contains(sessionType.ToUpper(), result.Title?.ToUpperInvariant() ?? string.Empty);
    }
}