using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Presentation.Parsing;

namespace PersonalFinanceCli.Tests;

public sealed class CommandParserTests
{
    [Fact]
    public void Tokenizer_HandlesQuotes()
    {
        var tokens = Tokenizer.Tokenize("expense add 12.50 \"Coffee and tea\" --note \"at office\"");

        Assert.Equal(new[] { "expense", "add", "12.50", "Coffee and tea", "--note", "at office" }, tokens);
    }

    [Fact]
    public void Parse_Transaction_WithDateAndNote()
    {
        var parser = new CommandParser();

        var cmd = parser.Parse("expense add 12.50 \"Food\" --card 3 --date 2026-03-01 --note \"dinner out\"");

        var typed = Assert.IsType<TransactionAddCommand>(cmd);
        Assert.Equal(TransactionType.Expense, typed.Type);
        Assert.Equal(12.50m, typed.Amount);
        Assert.Equal("Food", typed.Category);
        Assert.Equal(3, typed.CardId);
        Assert.Equal(new DateOnly(2026, 3, 1), typed.Date);
        Assert.Equal("dinner out", typed.Note);
    }

    [Fact]
    public void Parse_InvalidDate_Throws()
    {
        var parser = new CommandParser();

        var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse("expense add 2 \"Food\" --date 2026-02-30"));

        Assert.Contains("Invalid --date", ex.Message);
    }

    [Fact]
    public void Parse_ExpenseEmptyCategoryAfterTrim_ThrowsInParser()
    {
        var parser = new CommandParser();

        var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse("expense add 2 \"   \""));

        Assert.Equal("Category cannot be empty.", ex.Message);
    }

    [Fact]
    public void Parse_IncomeKeepsCategoryWhitespace_AsIs()
    {
        var parser = new CommandParser();

        var cmd = parser.Parse("income add 5 \"  Salary  \"");

        var typed = Assert.IsType<TransactionAddCommand>(cmd);
        Assert.Equal("  Salary  ", typed.Category);
    }
}
