namespace PersonalFinanceCli.Tests;

public sealed class DivergenceAndDuplicationTests
{
    [Fact]
    public void ExpenseAndIncome_UseDifferentDefaultSources_WhenSourcesDiverge()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3));
        app.Run("card", "add", "A", "RUB", "0");
        app.Run("card", "add", "B", "RUB", "0");

        var data = app.Store.Load();
        data.Cards.Single(c => c.Id == 1).IsDefault = true;
        data.Cards.Single(c => c.Id == 2).IsDefault = false;
        data.DefaultCardId = CardIdToGuid(2);
        app.Store.Save(data);

        Assert.Equal(0, app.Run("expense", "add", "5", "Food"));
        Assert.Equal(0, app.Run("income", "add", "7", "Salary"));

        var tx = app.TransactionRepository.GetAll();
        Assert.Equal(2, tx[0].CardId); // expense via DefaultCardId
        Assert.Equal(1, tx[1].CardId); // income via IsDefault
    }

    [Fact]
    public void CushionLookup_ByContains_WorksWithoutExactName()
    {
        using var app = new TestAppContext(
            new DateOnly(2026, 3, 3),
            new string?[] { "income add 10 Bonus --card 1 --date 2026-03-03", "y", "1", "exit" });

        app.Run("card", "add", "Main", "RUB", "0");
        app.Run("card", "add", "my cushion savings", "RUB", "0");
        app.RunInteractive();

        Assert.DoesNotContain("Cushion account not found. Create now? (y/n)", app.Output);
        Assert.Contains(app.TransactionRepository.GetAll(), t => t.CardId == 2 && t.Category == "Transfer from income");
    }

    [Fact]
    public void Onboarding_HasSeenTrue_AndNoCards_DoesNotAsk()
    {
        using var app = new TestAppContext(new DateOnly(2026, 3, 3), new[] { "exit" });
        var data = app.Store.Load();
        data.HasSeenOnboarding = true;
        data.Cards.Clear();
        app.Store.Save(data);

        app.RunInteractive();
        Assert.DoesNotContain("Create 'Financial cushion' account? (y/n)", app.Output);
    }

    [Fact]
    public void CommandParserAndWizardCollector_ParseOptionsDifferently()
    {
        var parser = new PersonalFinanceCli.Presentation.Parsing.CommandParser();
        var cmd = parser.Parse("income add 1 Salary --date 2026-3-3 --note plain");
        var parsed = Assert.IsType<PersonalFinanceCli.Presentation.Parsing.TransactionAddCommand>(cmd);
        Assert.Equal(new DateOnly(2026, 3, 3), parsed.Date);
        Assert.Equal("plain", parsed.Note);

        var collector = new PersonalFinanceCli.Presentation.Parsing.WizardOptionCollector();
        var failDate = collector.Collect(new[] { "--date", "2026-3-3" }, 0);
        Assert.Equal("Invalid --date value. Use strict YYYY-MM-DD.", failDate.Error);

        var failNote = collector.Collect(new[] { "--note", "plain" }, 0);
        Assert.Equal("Wizard requires quoted note for --note.", failNote.Error);
    }

    private static Guid CardIdToGuid(int cardId)
    {
        var raw = cardId.ToString("D12");
        return Guid.Parse($"00000000-0000-0000-0000-{raw}");
    }
}
