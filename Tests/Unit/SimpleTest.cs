using Xunit;

namespace finaid.Tests.Unit;

public class SimpleTest
{
    [Fact]
    public void BasicTest()
    {
        // Arrange
        var expected = 4;
        
        // Act
        var actual = 2 + 2;
        
        // Assert
        Assert.Equal(expected, actual);
    }
}