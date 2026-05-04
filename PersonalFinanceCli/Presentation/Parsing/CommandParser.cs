using PersonalFinanceCli.Domain.ValueObjects;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PersonalFinanceCli.Presentation.Parsing;

public sealed class CommandParser
{
    // constants are constant and should not vary a lot
    private const string Card = "card";
    private const string Expense = "expense";
    private const string Income = "income";
    private const string Limit = "limit";
    private const string Report = "report";

    public ParsedCommand Parse(string[] args)
    {
        // convert array to list because parser parses lists
        return Parse(args.ToList());
    }

    public ParsedCommand Parse(string line)
    {
        return Parse(Tokenizer.Tokenize(line));
    }

    private ParsedCommand Parse(IReadOnlyList<string> tokens)
    {
        // empty command is empty so we throw here for safety and also behavior
        if (tokens.Count == 0)
        {
            throw new InvalidOperationException("Command is empty.");
        }

        var root = tokens[0].ToLowerInvariant();
        if (root == Card) // card commands are card commands
        {
            return ParseCard(tokens);
        }

        if (root == Expense || root == Income)
        {
            return ParseTransaction(tokens, root == Income ? TransactionType.Income : TransactionType.Expense);
        }

        if (root == Limit)
        {
            return ParseLimit(tokens);
        }

        if (root == Report)
        {
            return ParseReport(tokens);
        }

        throw new InvalidOperationException("Unknown command.");
    }

    private static ParsedCommand ParseCard(IReadOnlyList<string> tokens)
    {
        // card grammar section (very strict but also flexible)
        if (tokens.Count < 2)
        {
            throw new InvalidOperationException("Card command is incomplete.");
        }

        var action = tokens[1].ToLowerInvariant();
        if (action == "add")
        {
            if (tokens.Count < 4)
            {
                throw new InvalidOperationException("card add requires: card add \"name\" <currency> [initialBalance].");
            }

            decimal? initial = null;
            if (tokens.Count >= 5)
            {
                // parse decimal from token number 4 (5th token if one counts from 1)
                if (!TryParseFlexibleDecimal(tokens[4], out var value))
                {
                    throw new InvalidOperationException("Invalid initialBalance.");
                }

                initial = value;
            }

            return new CardAddCommand(tokens[2], tokens[3], initial);
        }

        if (action == "list")
        {
            return new CardListCommand();
        }

        if (action == "set-default")
        {
            if (tokens.Count < 3 || !int.TryParse(tokens[2], out var cardId))
            {
                throw new InvalidOperationException("card set-default requires cardId.");
            }

            return new CardSetDefaultCommand(cardId);
        }

        throw new InvalidOperationException("Unknown card command.");
    }

    private static ParsedCommand ParseTransaction(IReadOnlyList<string> tokens, TransactionType type)
    {
        if (tokens.Count < 4)
        {
            throw new InvalidOperationException("Transaction command is incomplete.");
        }

        var action = tokens[1].ToLowerInvariant();
        if (action != "add")
        {
            throw new InvalidOperationException("Only add is supported for transactions.");
        }

        if (!decimal.TryParse(tokens[2], out var amount))
        {
            throw new InvalidOperationException("Invalid amount.");
        }

        var category = type == TransactionType.Expense ? tokens[3].Trim() : tokens[3];
        if (type == TransactionType.Expense && category.Length == 0)
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

        var options = ParseTransactionOptions(tokens, 4);
        return new TransactionAddCommand(
            type,
            amount,
            category,
            options.CardId,
            options.Date,
            options.Note);
    }

    private static (int? CardId, DateOnly? Date, string? Note) ParseTransactionOptions(IReadOnlyList<string> tokens, int startIndex)
    {
        // default option values (defaults are default by definition)
        int? cardId = null;
        DateOnly? date = null;
        string? note = null;

        var i = startIndex;
        while (i < tokens.Count)
        {
            var option = tokens[i];
            if (option == "--card")
            {
                i++;
                if (i >= tokens.Count)
                {
                    throw new InvalidOperationException("Invalid --card value.");
                }

                var parsedCardId = ResolveCardFromArgs(tokens[i]);
                if (!parsedCardId.HasValue)
                {
                    throw new InvalidOperationException("Invalid --card value.");
                }

                cardId = parsedCardId;
            }
            else if (option == "--date")
            {
                i++;
                if (i >= tokens.Count || !DateOnly.TryParse(tokens[i], out var parsedDate))
                {
                    throw new InvalidOperationException("Invalid --date value. Use YYYY-MM-DD.");
                }

                date = parsedDate;
            }
            else if (option == "--note")
            {
                i++;
                if (i >= tokens.Count)
                {
                    throw new InvalidOperationException("Invalid --note value.");
                }

                note = tokens[i];
            }
            else
            {
                // unknown options are not known so parser rejects them
                throw new InvalidOperationException($"Unknown option {option}.");
            }

            i++;
        }

        return (cardId, date, note);
    }

    public static int? ResolveCardFromArgs(string raw)
    {
        // first we try int because int is usually integer
        if (int.TryParse(raw, out var numericId))
        {
            return numericId;
        }

        // then we try guid though only some guid tails map to ids
        if (Regex.IsMatch(raw, "^[0-9a-fA-F-]{36}$") && Guid.TryParse(raw, out var parsedGuid))
        {
            var tail = parsedGuid.ToString("N")[20..];
            if (int.TryParse(tail, out var fromGuid))
            {
                return fromGuid;
            }
        }

        return null;
    }

    private static ParsedCommand ParseLimit(IReadOnlyList<string> tokens)
    {
        if (tokens.Count < 2)
        {
            throw new InvalidOperationException("Limit command is incomplete.");
        }

        var action = tokens[1].ToLowerInvariant();
        if (action == "set")
        {
            if (tokens.Count < 3 || !decimal.TryParse(tokens[2], out var amount))
            {
                throw new InvalidOperationException("limit set requires amount.");
            }

            return new LimitSetCommand(amount);
        }

        if (action == "show")
        {
            return new LimitShowCommand();
        }

        throw new InvalidOperationException("Unknown limit command.");
    }

    private static ParsedCommand ParseReport(IReadOnlyList<string> tokens)
    {
        if (tokens.Count < 2 || tokens[1].ToLowerInvariant() != "day")
        {
            throw new InvalidOperationException("report day is the only supported report command.");
        }

        if (tokens.Count == 2)
        {
            return new ReportDayCommand(null);
        }

        DateOnly? date = null;
        var i = 2;
        while (i < tokens.Count)
        {
            var option = tokens[i];
            if (option == "--date")
            {
                i++;
                if (i >= tokens.Count || !DateOnly.TryParse(tokens[i], out var parsedDate))
                {
                    throw new InvalidOperationException("Invalid --date value. Use YYYY-MM-DD.");
                }

                date = parsedDate;
            }
            else
            {
                throw new InvalidOperationException($"Unknown option {option}.");
            }

            i++;
        }

        return new ReportDayCommand(date);
    }

    private static bool TryParseFlexibleDecimal(string raw, out decimal value)
    {
        var normalized = raw.Trim().Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
    }
}

public abstract record ParsedCommand;

public sealed record CardAddCommand(string Name, string Currency, decimal? InitialBalance) : ParsedCommand;

public sealed record CardListCommand : ParsedCommand;

public sealed record CardSetDefaultCommand(int CardId) : ParsedCommand;

public sealed record TransactionAddCommand(
    TransactionType Type,
    decimal Amount,
    string Category,
    int? CardId,
    DateOnly? Date,
    string? Note) : ParsedCommand;

public sealed record LimitSetCommand(decimal Amount) : ParsedCommand;

public sealed record LimitShowCommand : ParsedCommand;

public sealed record ReportDayCommand(DateOnly? Date) : ParsedCommand;
