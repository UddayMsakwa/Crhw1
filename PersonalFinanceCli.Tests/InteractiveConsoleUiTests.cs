namespace PersonalFinanceCli.Tests;

public sealed class InteractiveConsoleUiTests
{
    [Fact]
    public void Repl_Help_WizardFlow_AndExit_WorkInSingleSession()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[]
            {
                "n",
                "help",
                "card add",
                "Tinkoff",
                "RUB",
                "1000",
                "expense add",
                "12.50",
                "Coffee and tea",
                "",
                "2026-03-03",
                "exit"
            });

        app.RunInteractive();

        Assert.Contains("Commands:", app.Output);
        Assert.Contains("Currency (RUB/EUR)? ", app.Output);
        Assert.Contains("Amount? ", app.Output);
        Assert.Contains("Category? ", app.Output);
        Assert.Contains("Card? (enter to use default, id or name) ", app.Output);
        Assert.Contains("Date? (YYYY-MM-DD, enter = today) ", app.Output);
        Assert.Contains("Date: 2026-03-03", app.Output);
        Assert.Single(app.TransactionRepository.GetAll());
    }

    [Fact]
    public void Repl_CancelInWizard_DoesNotPersistChanges()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[]
            {
                "n",
                "card add",
                "MyCard",
                "cancel",
                "exit"
            });

        app.RunInteractive();

        Assert.Contains("Cancelled.", app.Output);
        Assert.Empty(app.CardRepository.GetAll());
    }

    [Fact]
    public void Repl_InvalidWizardValues_ReasksUntilValid()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[]
            {
                "n",
                "card add",
                "Main",
                "USD",
                "RUB",
                "",
                "expense add",
                "abc",
                "10",
                "Food",
                "x",
                "1",
                "wrong",
                "2026-03-03",
                "exit"
            });

        app.RunInteractive();

        Assert.Contains("Error: Unknown currency. Allowed: RUB, EUR.", app.Output);
        Assert.Contains("Error: Invalid decimal.", app.Output);
        Assert.Contains("Error: Invalid card. Enter card id or card name.", app.Output);
        Assert.Contains("Error: Invalid date.", app.Output);
        Assert.Single(app.TransactionRepository.GetAll());
    }

    [Fact]
    public void Repl_ExpenseWizard_AcceptsCardName()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[]
            {
                "n",
                "card add Main RUB 0",
                "card add Cash RUB 0",
                "expense add 5 Food",
                "Cash",
                "2026-03-03",
                "exit"
            });

        app.RunInteractive();

        var trx = app.TransactionRepository.GetAll().Single();
        Assert.Equal(2, trx.CardId);
    }

    [Fact]
    public void Repl_EmptyLine_OnlyShowsNextPrompt()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[]
            {
                "",
                "",
                "exit"
            });

        app.RunInteractive();

        var promptCount = CountOccurrences(app.Output, "> ");
        Assert.Equal(2, promptCount);
    }

    [Fact]
    public void Repl_UnknownCommand_ShowsHint()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[]
            {
                "n",
                "abracadabra",
                "exit"
            });

        app.RunInteractive();

        Assert.Contains("Error: Unknown command.", app.Output);
        Assert.Contains("type help", app.Output);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var start = 0;
        while (true)
        {
            var index = source.IndexOf(value, start, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            start = index + value.Length;
        }
    }
}
