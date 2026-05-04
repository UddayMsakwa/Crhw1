using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Tests;

public sealed class CardAndTransactionFlowTests
{
    [Fact]
    public void CardAdd_ThenList_AndSetDefault_WorkAndShowDefault()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        Assert.Equal(0, app.Run("card", "add", "Tinkoff", "RUB", "1000"));
        Assert.Contains("Date: 2026-03-03", app.Output);

        Assert.Equal(0, app.Run("card", "add", "Cash", "RUB", "20"));
        Assert.Equal(0, app.Run("card", "set-default", "2"));
        Assert.Contains("(default)", app.Output);

        Assert.Equal(0, app.Run("card", "list"));
        Assert.Contains("1: Tinkoff", app.Output);
        Assert.Contains("2: Cash (default)", app.Output);
    }

    [Fact]
    public void ExpenseWithExplicitCard_UsesThatCard()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB", "0");
        app.Run("card", "add", "B", "RUB", "0");

        Assert.Equal(0, app.Run("expense", "add", "10", "Food", "--card", "2"));

        var tx = app.TransactionRepository.GetAll().Single();
        Assert.Equal(2, tx.CardId);
    }

    [Fact]
    public void IncomeWithoutCard_UsesDefaultCard()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB", "0");
        app.Run("card", "add", "B", "RUB", "0");
        app.Run("card", "set-default", "2");

        Assert.Equal(0, app.Run("income", "add", "100", "Salary"));

        var tx = app.TransactionRepository.GetAll().Single();
        Assert.Equal(2, tx.CardId);
    }

    [Fact]
    public void WithoutCardSpecified_UsesFirstCard_WhenNoDefaultExists()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB", "0");
        app.Run("card", "add", "B", "RUB", "0");

        var data = app.Store.Load();
        foreach (var c in data.Cards)
        {
            c.IsDefault = false;
        }

        app.Store.Save(data);

        Assert.Equal(0, app.Run("expense", "add", "4", "Taxi"));

        var tx = app.TransactionRepository.GetAll().Single();
        Assert.Equal(1, tx.CardId);
    }

    [Fact]
    public void ReportBalance_UsesAllDates_NotOnlyReportDay()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "Tinkoff", "RUB", "1000");
        app.Run("income", "add", "200", "Salary", "--date", "2026-03-01");
        app.Run("expense", "add", "50", "Food", "--date", "2026-03-02");

        Assert.Equal(0, app.Run("report", "day", "--date", "2026-03-03"));
        Assert.Contains("Tinkoff (default): 1150.00 RUB", app.Output);
    }

    [Fact]
    public void CardAdd_InitialBalance_WithComma_IsApplied()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        Assert.Equal(0, app.Run("card", "add", "Main", "RUB", "1000,50"));
        Assert.Equal(0, app.Run("report", "day", "--date", "2026-03-03"));
        Assert.Contains("Main (default): 1000.50 RUB", app.Output);
    }

    [Fact]
    public void AutoReport_AfterStateChangingCommand_UsesTodayFromClock_NotOperationDate()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "Tinkoff", "RUB", "0");

        Assert.Equal(0, app.Run("expense", "add", "12.50", "Food", "--date", "2026-03-01"));
        Assert.Contains("Date: 2026-03-03", app.Output);
        Assert.Contains("Expense: 0.00 RUB", app.Output);
    }
}
