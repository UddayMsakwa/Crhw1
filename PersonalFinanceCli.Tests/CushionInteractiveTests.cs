namespace PersonalFinanceCli.Tests;

public sealed class CushionInteractiveTests
{
    [Fact]
    public void Onboarding_FirstRun_Yes_CreatesCushionCard()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "y", "exit" });

        app.RunInteractive();

        var cards = app.CardRepository.GetAll();
        Assert.Single(cards);
        Assert.Equal("Financial cushion", cards[0].Name);
        Assert.Equal(0m, cards[0].InitialBalance);
    }

    [Fact]
    public void Onboarding_FirstRun_No_DoesNotCreateCard()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "n", "exit" });

        app.RunInteractive();

        Assert.Empty(app.CardRepository.GetAll());
    }

    [Fact]
    public void Onboarding_SecondLaunch_WithinTwoWeeks_DoesNotAskAgain()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pfcli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            using (var app1 = new TestAppContext(
                       new DateOnly(2026, 3, 3),
                       new string?[] { "n", "card add Main RUB 0", "exit" },
                       existingDirectory: dir,
                       keepDirectory: true))
            {
                app1.RunInteractive();
            }

            using var app2 = new TestAppContext(
                new DateOnly(2026, 3, 3),
                new string?[] { "help", "exit" },
                existingDirectory: dir,
                keepDirectory: true);

            app2.RunInteractive();

            Assert.DoesNotContain("Create 'Financial cushion' account? (y/n)", app2.Output);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void Onboarding_AfterTwoWeeks_AsksAgain_WhenNoCushion()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pfcli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            using (var app1 = new TestAppContext(
                       new DateOnly(2026, 3, 3),
                       new string?[] { "n", "card add Main RUB 0", "exit" },
                       existingDirectory: dir,
                       keepDirectory: true))
            {
                app1.RunInteractive();
            }

            using var app2 = new TestAppContext(
                new DateOnly(2026, 3, 17),
                new string?[] { "n", "exit" },
                existingDirectory: dir,
                keepDirectory: true);

            app2.RunInteractive();

            Assert.Contains("Create 'Financial cushion' account? (y/n)", app2.Output);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void IncomeAdd_TransferQuestion_No_KeepsOnlyIncome()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "n", "income add 100 Salary --card 1 --date 2026-03-03", "n", "exit" });
        app.Run("card", "add", "Main", "RUB", "0");

        app.RunInteractive();

        var all = app.TransactionRepository.GetAll();
        Assert.Single(all);
        Assert.Equal(Domain.ValueObjects.TransactionType.Income, all[0].Type);
        Assert.Contains("Transfer part of income to 'Financial cushion'? (y/n)", app.Output);
        Assert.Contains("Date: 2026-03-03", app.Output);
    }

    [Fact]
    public void IncomeAdd_NoCushion_CreateNow_No_DoesNotTransfer()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "n", "income add 100 Bonus --card 1 --date 2026-03-03", "y", "n", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.RunInteractive();

        var all = app.TransactionRepository.GetAll();
        Assert.Single(all);
        Assert.Contains("Cushion account not found. Create now? (y/n)", app.Output);
    }

    [Fact]
    public void IncomeAdd_NoCushion_CreateNow_Yes_PercentTransfer_RoundsDown()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "n", "income add 10.01 Bonus --card 1 --date 2026-03-03", "y", "y", "25%", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.RunInteractive();

        var all = app.TransactionRepository.GetAll();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, t => t.Type == Domain.ValueObjects.TransactionType.Expense && t.Category == "Transfer to cushion" && t.Amount == 2.50m);
        Assert.Contains(all, t => t.Type == Domain.ValueObjects.TransactionType.Income && t.Category == "Transfer from income" && t.Amount == 2.50m);
    }

    [Fact]
    public void IncomeAdd_EmptyTransferAmount_Uses20PercentForSalary()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "n", "income add 100 \"Salary March\" --card 1 --date 2026-03-03", "y", "y", "", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.RunInteractive();

        var transferExpense = app.TransactionRepository.GetAll()
            .Single(t => t.Type == Domain.ValueObjects.TransactionType.Expense && t.Category == "Transfer to cushion");
        Assert.Equal(20.00m, transferExpense.Amount);
    }

    [Fact]
    public void IncomeAdd_TransferByAbsoluteAmount_UsesNormalRounding()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "n", "income add 50 Bonus --card 1 --date 2026-03-03", "y", "y", "12.345", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.RunInteractive();

        var transferExpense = app.TransactionRepository.GetAll()
            .Single(t => t.Type == Domain.ValueObjects.TransactionType.Expense && t.Category == "Transfer to cushion");
        Assert.Equal(12.35m, transferExpense.Amount);
    }

    [Fact]
    public void IncomeAdd_InvalidTransferAmount_RepeatsQuestion()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "n", "income add 10 Bonus --card 1 --date 2026-03-03", "y", "y", "0", "99", "abc", "1.23", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.RunInteractive();

        var transferExpense = app.TransactionRepository.GetAll()
            .Single(t => t.Type == Domain.ValueObjects.TransactionType.Expense && t.Category == "Transfer to cushion");

        Assert.Equal(1.23m, transferExpense.Amount);
        Assert.Contains("Error: Transfer amount must be > 0 and <= income", app.Output);
        Assert.Contains("Error: Invalid transfer amount.", app.Output);
    }

    [Fact]
    public void IncomeAdd_CurrencyMismatch_No_CancelsOnlyTransfer()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "income add 20 Work --card 1 --date 2026-03-03", "y", "n", "exit" });

        app.Run("card", "add", "EUR Card", "EUR", "0");
        app.Run("card", "add", "Financial cushion", "RUB", "0");
        app.RunInteractive();

        var all = app.TransactionRepository.GetAll();
        Assert.Single(all);
        Assert.Equal(Domain.ValueObjects.TransactionType.Income, all[0].Type);
        Assert.Contains("Currencies do not match. Transfer anyway? (y/n)", app.Output);
    }

    [Fact]
    public void IncomeAdd_CurrencyMismatch_Yes_TransfersWithoutConversion()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "income add 20 Work --card 1 --date 2026-03-03", "y", "y", "5", "exit" });

        app.Run("card", "add", "EUR Card", "EUR", "0");
        app.Run("card", "add", "Financial cushion", "RUB", "0");
        app.RunInteractive();

        var all = app.TransactionRepository.GetAll();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, t => t.CardId == 2 && t.Type == Domain.ValueObjects.TransactionType.Income && t.Amount == 5m);
    }

    [Fact]
    public void IncomeAdd_SmallIncome_DefaultTransferIsOne()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "income add 5 Gift --card 1 --date 2026-03-03", "y", "", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.Run("card", "add", "Financial cushion", "RUB", "0");
        app.RunInteractive();

        var transferExpense = app.TransactionRepository.GetAll()
            .Single(t => t.Type == Domain.ValueObjects.TransactionType.Expense && t.Category == "Transfer to cushion");
        Assert.Equal(1m, transferExpense.Amount);
    }

    [Fact]
    public void IncomeAdd_TransferAmountCancel_CancelsOnlyTransfer()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "n", "income add 30 Bonus --card 1 --date 2026-03-03", "y", "y", "cancel", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.RunInteractive();

        var all = app.TransactionRepository.GetAll();
        Assert.Single(all);
        Assert.Equal(Domain.ValueObjects.TransactionType.Income, all[0].Type);
        Assert.Contains("Transfer cancelled.", app.Output);
        Assert.Contains("Date: 2026-03-03", app.Output);
    }

    [Fact]
    public void IncomeAdd_WithTransfer_ChangesBalancesOfSourceAndCushion()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "income add 100 Bonus --card 1 --date 2026-03-03", "y", "", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.Run("card", "add", "Financial cushion", "RUB", "0");
        app.RunInteractive();

        var tx = app.TransactionRepository.GetAll();
        var mainBalance = BalanceForCard(1, 0m, tx);
        var cushionBalance = BalanceForCard(2, 0m, tx);

        Assert.Equal(90.00m, mainBalance);
        Assert.Equal(10.00m, cushionBalance);
        Assert.Contains("Date: 2026-03-03", app.Output);
    }

    private static decimal BalanceForCard(int cardId, decimal initial, IReadOnlyList<Domain.Entities.Transaction> tx)
    {
        var result = initial;
        foreach (var t in tx.Where(t => t.CardId == cardId))
        {
            result = t.Type == Domain.ValueObjects.TransactionType.Income ? result + t.Amount : result - t.Amount;
        }

        return result;
    }
}
