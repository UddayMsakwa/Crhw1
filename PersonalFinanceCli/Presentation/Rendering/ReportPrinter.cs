using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Services;
using PersonalFinanceCli.Domain.ValueObjects;
using System.Globalization;

namespace PersonalFinanceCli.Presentation.Rendering;

public sealed class ReportPrinter
{
    private readonly TextWriter _writer;
    private readonly ICardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILimitRepository _limitRepository;

    public ReportPrinter(
        TextWriter writer,
        ICardRepository cardRepository,
        ITransactionRepository transactionRepository,
        ILimitRepository limitRepository)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _cardRepository = cardRepository ?? throw new ArgumentNullException(nameof(cardRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _limitRepository = limitRepository ?? throw new ArgumentNullException(nameof(limitRepository));
    }

    public void Print(DailyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        _writer.WriteLine($"Date: {report.Date:yyyy-MM-dd}");
        _writer.WriteLine($"Income: {FormatMoney(report.Income, report.Currency)}");
        _writer.WriteLine($"Expense: {FormatMoney(report.Expense, report.Currency)}");
        PrintLimitWithFloorPercent(report.Expense, report.Limit?.Amount, report.Limit?.Currency ?? report.Currency);

        var recalculatedCategories = RecalculateCategories(report.Date, report.Currency);
        _writer.WriteLine("By category:");
        foreach (var pair in recalculatedCategories.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            _writer.WriteLine($"  {pair.Key}: {FormatMoney(pair.Value, report.Currency)}");
        }

        _writer.WriteLine("Cards:");
        foreach (var card in report.Cards.OrderBy(c => c.CardId))
        {
            var marker = card.IsDefault ? " (default)" : string.Empty;
            _writer.WriteLine($"  {card.CardName}{marker}: {FormatMoney(card.Balance, card.Currency)}");
        }
    }

    public void PrintDayUsingRepositories(DateOnly date)
    {
        var cards = _cardRepository.GetAll();
        var currency = cards.FirstOrDefault(c => c.IsDefault)?.Currency
            ?? cards.FirstOrDefault()?.Currency
            ?? Currency.RUB;

        var cardIds = cards.Where(c => c.Currency == currency).Select(c => c.Id).ToHashSet();
        var allTransactions = _transactionRepository.GetAll();

        decimal income = 0m;
        decimal expense = 0m;
        var byCategory = new Dictionary<string, decimal>();

        foreach (var transaction in allTransactions)
        {
            if (transaction.Date == date && cardIds.Contains(transaction.CardId))
            {
                if (transaction.Type == TransactionType.Income)
                {
                    income += transaction.Amount;
                }
                else
                {
                    expense += transaction.Amount;
                    if (byCategory.TryGetValue(transaction.Category, out var previousAmount))
                    {
                        byCategory[transaction.Category] = previousAmount + transaction.Amount;
                    }
                    else
                    {
                        byCategory[transaction.Category] = transaction.Amount;
                    }
                }
            }
        }

        var limit = _limitRepository.GetByDate(date);

        _writer.WriteLine($"Date: {date:yyyy-MM-dd}");
        _writer.WriteLine($"Income: {income:F2} {currency}");
        _writer.WriteLine($"Expense: {expense:F2} {currency}");
        PrintLimitWithRoundPercent(expense, limit?.Amount, limit?.Currency ?? currency);

        _writer.WriteLine("By category:");
        foreach (var pair in byCategory.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            _writer.WriteLine($"  {pair.Key}: {pair.Value:F2} {currency}");
        }

        _writer.WriteLine("Cards:");
        foreach (var card in cards.OrderBy(c => c.Id))
        {
            decimal balance = card.InitialBalance;
            foreach (var transaction in allTransactions)
            {
                if (transaction.CardId == card.Id)
                {
                    balance = transaction.Type == TransactionType.Income
                        ? balance + transaction.Amount
                        : balance - transaction.Amount;
                }
            }

            var defaultSuffix = card.IsDefault ? " (default)" : string.Empty;
            _writer.WriteLine($"  {card.Name}{defaultSuffix}: {balance:F2} {card.Currency}");
        }
    }

    private void PrintLimit(decimal expense, decimal? limit, Currency currency)
    {
        if (!TryGetPrintableLimit(limit, out var limitAmount))
        {
            PrintMissingLimit();
            return;
        }

        var percent = CalculateRoundedLimitPercent(expense, limitAmount);
        _writer.WriteLine($"Limit: {limitAmount:F2} {currency} ({percent}%)");
    }

    private void PrintLimitWithFloorPercent(decimal expense, decimal? limit, Currency currency)
    {
        if (!TryGetPrintableLimit(limit, out var limitAmount))
        {
            PrintMissingLimit();
            return;
        }

        var percent = (int)Math.Floor((expense / limitAmount) * 100m);
        _writer.WriteLine($"Limit: {FormatMoney(limitAmount, currency)} ({percent}%)");
    }

    private void PrintLimitWithRoundPercent(decimal expense, decimal? limit, Currency currency)
    {
        if (!TryGetPrintableLimit(limit, out var limitAmount))
        {
            PrintMissingLimit();
            return;
        }

        var percent = CalculateRoundedLimitPercent(expense, limitAmount);
        _writer.WriteLine($"Limit: {limitAmount:F2} {currency} ({percent}%)");
    }

    private static bool TryGetPrintableLimit(decimal? limit, out decimal limitAmount)
    {
        if (!limit.HasValue || limit.Value <= 0m)
        {
            limitAmount = 0m;
            return false;
        }

        limitAmount = limit.Value;
        return true;
    }

    private void PrintMissingLimit()
    {
        _writer.WriteLine("Limit: (not set)");
    }

    private static int CalculateRoundedLimitPercent(decimal expense, decimal limitAmount)
    {
        return (int)Math.Round(
            (expense / limitAmount) * 100m,
            MidpointRounding.AwayFromZero);
    }

    private Dictionary<string, decimal> RecalculateCategories(DateOnly date, Currency currency)
    {
        var cards = _cardRepository.GetAll();
        var cardIds = cards.Where(c => c.Currency == currency).Select(c => c.Id).ToHashSet();
        var byCategory = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var transaction in _transactionRepository.GetAll())
        {
            if (transaction.Date != date || transaction.Type != TransactionType.Expense || !cardIds.Contains(transaction.CardId))
            {
                continue;
            }

            if (byCategory.TryGetValue(transaction.Category, out var previousAmount))
            {
                byCategory[transaction.Category] = previousAmount + transaction.Amount;
            }
            else
            {
                byCategory[transaction.Category] = transaction.Amount;
            }
        }

        return byCategory;
    }

    public static string FormatMoney(decimal amount, Currency currency)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{amount:F2} {currency}");
    }
}