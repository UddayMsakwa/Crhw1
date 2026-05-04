namespace PersonalFinanceCli.Tests;

public sealed class ValidationAndErrorTests
{
    [Fact]
    public void Error_WhenNoCardsForTransaction()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        Assert.Equal(1, app.Run("expense", "add", "10", "Food"));
        Assert.Contains("Error: No cards available.", app.Output);
    }

    [Fact]
    public void Error_WhenUnknownCardProvided()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB", "0");

        Assert.Equal(1, app.Run("income", "add", "10", "Salary", "--card", "999"));
        Assert.Contains("Error: Card not found.", app.Output);
    }

    [Fact]
    public void Error_InvalidCurrency()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        Assert.Equal(1, app.Run("card", "add", "A", "USD"));
        Assert.Contains("Error: Unknown currency", app.Output);
    }

    [Fact]
    public void Error_InvalidDateInCommand()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB");

        Assert.Equal(1, app.Run("income", "add", "10", "Salary", "--date", "wrong"));
        Assert.Contains("Error: Invalid --date", app.Output);
    }

    [Fact]
    public void Error_AmountMustBePositive()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB");

        Assert.Equal(1, app.Run("expense", "add", "0", "Food"));
        Assert.Contains("Error: Amount must be > 0.", app.Output);
    }

    [Fact]
    public void Error_EmptyCategoryForIncome_ComesFromHandler()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB");

        Assert.Equal(1, app.Run("income", "add", "1", "   "));
        Assert.Contains("Error: Category cannot be empty.", app.Output);
    }

    [Fact]
    public void Error_SetLimitWithZero()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB");

        Assert.Equal(1, app.Run("limit", "set", "0"));
        Assert.Contains("Error: Limit must be > 0.", app.Output);
    }

    [Fact]
    public void Error_LimitSetWithoutCards()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        Assert.Equal(1, app.Run("limit", "set", "100"));
        Assert.Contains("Error: Cannot set limit without cards.", app.Output);
    }
}
