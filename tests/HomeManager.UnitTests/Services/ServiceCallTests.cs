namespace HomeManager.Services;

using System.Text.Json.Nodes;

using HomeManager.Domain;

public sealed class ServiceCallTests
{
    [Fact]
    public void GetNumberAcceptsNumberAndNumericText()
    {
        // Arrange
        var call = Create(new JsonObject { ["a"] = 50, ["b"] = 12.5, ["c"] = "30", ["d"] = JsonNode.Parse("7") });

        // Act & Assert
        Assert.Equal(50, call.GetNumber("a"));
        Assert.Equal(12.5, call.GetNumber("b"));
        Assert.Equal(30, call.GetNumber("c"));
        Assert.Equal(7, call.GetNumber("d"));
        Assert.Null(call.GetNumber("missing"));
    }

    [Fact]
    public void GetNumberRejectsText()
    {
        // Arrange
        var call = Create(new JsonObject { ["a"] = "abc" });

        // Act & Assert
        Assert.Throws<ServiceValidationException>(() => call.GetNumber("a"));
    }

    [Fact]
    public void GetFlagAndText()
    {
        // Arrange
        var call = Create(new JsonObject { ["flag"] = false, ["text"] = "true", ["number"] = 1 });

        // Act & Assert
        Assert.False(call.GetFlag("flag"));
        Assert.True(call.GetFlag("text"));
        Assert.Equal("true", call.GetText("text"));
        Assert.Equal("1", call.GetText("number"));
        Assert.Throws<ServiceValidationException>(() => call.GetFlag("number"));
    }

    private static ServiceCall Create(JsonObject data) =>
        new("test", "test", data, ServiceTarget.Empty, EventContext.Create());
}
