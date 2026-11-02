namespace HomeManager;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

public sealed class SettingValidationTests
{
    // 初期パスワードが空なら起動しない (入れ子の設定も [ValidateObjectMembers] で確かめる)
    [Fact]
    public void EmptyInitialPasswordStopsStartup()
    {
        // Arrange
        using var factory = new TestApplicationFactory();
        using var invalid = factory.WithWebHostBuilder(static builder => builder.UseSetting("Auth:InitialAccount:Password", string.Empty));

        // Act
        var exception = Record.Exception(invalid.CreateClient);

        // Assert
        Assert.NotNull(FindOptionsValidationException(exception));
    }

    private static OptionsValidationException? FindOptionsValidationException(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is OptionsValidationException found)
            {
                return found;
            }

            exception = exception.InnerException;
        }

        return null;
    }
}
