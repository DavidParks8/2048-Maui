using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace TwentyFortyEight.Tests;

[TestClass]
public class BasicTests
{
    [TestMethod]
    public void TestMethod1()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TestMoqIsAvailable()
    {
        // Arrange & Act
        var mock = new Mock<ITestInterface>();
        mock.Setup(x => x.GetValue()).Returns(42);

        // Assert
        Assert.AreEqual(42, mock.Object.GetValue());
    }
}

public interface ITestInterface
{
    int GetValue();
}
