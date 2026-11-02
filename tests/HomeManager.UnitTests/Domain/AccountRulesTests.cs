namespace HomeManager.Domain;

public sealed class AccountRulesTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1234567", false)]
    [InlineData("12345678", true)]
    public void PasswordNeedsMinimumLength(string? value, bool valid)
    {
        // Act & Assert
        Assert.Equal(valid, AccountRules.ValidatePassword(value) is null);
    }

    [Fact]
    public void DisplayNameIsRequiredAndLimited()
    {
        // Act & Assert
        Assert.NotNull(AccountRules.ValidateDisplayName(" "));
        Assert.NotNull(AccountRules.ValidateDisplayName(new string('a', Length.Name + 1)));
        Assert.Null(AccountRules.ValidateDisplayName("まち"));
    }
}
