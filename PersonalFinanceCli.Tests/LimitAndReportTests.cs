namespace PersonalFinanceCli.Tests;

public sealed class LimitAndReportTests
{
    [Fact]
    public void LimitShow_NotSet()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        Assert.Equal(0, app.Run("limit", "show"));
        Assert.Contains("Limit: (not set)", app.Output);
    }

    [Fact]
    public void LimitSet_ThenShow_Works()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB");

        Assert.Equal(0, app.Run("limit", "set", "50"));
        Assert.Contains("Limit: 50.00 RUB (0%)", app.Output);

        Assert.Equal(0, app.Run("limit", "show"));
        Assert.Contains("Limit: 50.00 RUB (2026-03-03)", app.Output);
    }

    [Fact]
    public void Report_Percent_Exactly100()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB", "0");
        app.Run("limit", "set", "50");
        app.Run("expense", "add", "50", "Food");

        Assert.Equal(0, app.Run("report", "day"));
        Assert.Contains("Limit: 50.00 RUB (100%)", app.Output);
    }

    [Fact]
    public void Report_Percent_Over100()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB", "0");
        app.Run("limit", "set", "20");
        app.Run("expense", "add", "50", "Food");

        Assert.Equal(0, app.Run("report", "day"));
        Assert.Contains("Limit: 20.00 RUB (250%)", app.Output);
    }

    [Fact]
    public void Report_GroupsByCategory_AndMarksDefaultCard()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "Tinkoff", "RUB", "1000");
        app.Run("income", "add", "1000", "Salary");
        app.Run("expense", "add", "10", "Food");
        app.Run("expense", "add", "2.5", "Food");
        app.Run("expense", "add", "5", "Taxi");

        Assert.Equal(0, app.Run("report", "day"));
        Assert.Contains("Income: 1000.00 RUB", app.Output);
        Assert.Contains("Expense: 17.50 RUB", app.Output);
        Assert.Contains("Food: 12.50 RUB", app.Output);
        Assert.Contains("Taxi: 5.00 RUB", app.Output);
        Assert.Contains("Tinkoff (default): 1982.50 RUB", app.Output);
    }

    [Fact]
    public void ReportDay_PrintsOnce_NoAutoExtraReport()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB");

        Assert.Equal(0, app.Run("report", "day"));

        var occurrences = CountOccurrences(app.Output, "Date: 2026-03-03");
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void StateChangingCommands_AllPrintAutoReport()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        Assert.Equal(0, app.Run("card", "add", "A", "RUB"));
        Assert.Contains("Date: 2026-03-03", app.Output);

        Assert.Equal(0, app.Run("card", "set-default", "1"));
        Assert.Contains("Date: 2026-03-03", app.Output);

        Assert.Equal(0, app.Run("income", "add", "1", "Salary"));
        Assert.Contains("Date: 2026-03-03", app.Output);

        Assert.Equal(0, app.Run("expense", "add", "1", "Food"));
        Assert.Contains("Date: 2026-03-03", app.Output);

        Assert.Equal(0, app.Run("limit", "set", "10"));
        Assert.Contains("Date: 2026-03-03", app.Output);
    }

    [Fact]
    public void Report_ShowsNotSet_WhenStoredLimitIsZero()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));

        app.Run("card", "add", "A", "RUB");

        var data = app.Store.Load();
        data.DailyLimits.Add(new PersonalFinanceCli.Domain.Entities.DailyLimit
        {
            Id = 1,
            Date = new DateOnly(2026, 3, 3),
            Amount = 0,
            Currency = PersonalFinanceCli.Domain.ValueObjects.Currency.RUB
        });
        app.Store.Save(data);

        Assert.Equal(0, app.Run("report", "day"));
        Assert.Contains("Limit: (not set)", app.Output);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while (true)
        {
            index = text.IndexOf(value, index, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            count++;
            index += value.Length;
        }

        return count;
    }
}
