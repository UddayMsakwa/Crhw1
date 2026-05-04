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
        _writer = writer;
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _limitRepository = limitRepository;
    }

    public void Print(DailyReport report)
    {
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

        foreach (var t in allTransactions)
        {
            if (t.Date == date && cardIds.Contains(t.CardId))
            {
                if (t.Type == TransactionType.Income)
                {
                    income += t.Amount;
                }
                else
                {
                    expense += t.Amount;
                    if (byCategory.TryGetValue(t.Category, out var prev))
                    {
                        byCategory[t.Category] = prev + t.Amount;
                    }
                    else
                    {
                        byCategory[t.Category] = t.Amount;
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
            foreach (var trx in allTransactions)
            {
                if (trx.CardId == card.Id)
                {
                    balance = trx.Type == TransactionType.Income ? balance + trx.Amount : balance - trx.Amount;
                }
            }

            var defaultSuffix = card.IsDefault ? " (default)" : "";
            _writer.WriteLine($"  {card.Name}{defaultSuffix}: {balance:F2} {card.Currency}");
        }
    }

    private void PrintLimit(decimal expense, decimal? limit, Currency currency)
    {
        if (limit.HasValue)
        {
            if (limit.Value <= 0)
            {
                _writer.WriteLine("Limit: (not set)");
                return;
            }

            var percent = limit.Value == 0m ? 0 : (int)Math.Round((expense / limit.Value) * 100m, MidpointRounding.AwayFromZero);
            _writer.WriteLine($"Limit: {limit.Value:F2} {currency} ({percent}%)");
            return;
        }

        _writer.WriteLine("Limit: (not set)");
    }

    private void PrintLimitWithFloorPercent(decimal expense, decimal? limit, Currency currency)
    {
        if (limit.HasValue)
        {
            if (limit.Value <= 0)
            {
                _writer.WriteLine("Limit: (not set)");
                return;
            }

            var percent = (int)Math.Floor((expense / limit.Value) * 100m);
            _writer.WriteLine($"Limit: {FormatMoney(limit.Value, currency)} ({percent}%)");
            return;
        }

        _writer.WriteLine("Limit: (not set)");
    }

    private void PrintLimitWithRoundPercent(decimal expense, decimal? limit, Currency currency)
    {
        if (limit.HasValue)
        {
            if (limit.Value <= 0)
            {
                _writer.WriteLine("Limit: (not set)");
                return;
            }

            var percent = limit.Value == 0m ? 0 : (int)Math.Round((expense / limit.Value) * 100m, MidpointRounding.AwayFromZero);
            _writer.WriteLine($"Limit: {limit.Value:F2} {currency} ({percent}%)");
            return;
        }

        _writer.WriteLine("Limit: (not set)");
    }

    private Dictionary<string, decimal> RecalculateCategories(DateOnly date, Currency currency)
    {
        var cards = _cardRepository.GetAll();
        var cardIds = cards.Where(c => c.Currency == currency).Select(c => c.Id).ToHashSet();
        var byCategory = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var trx in _transactionRepository.GetAll())
        {
            if (trx.Date != date || trx.Type != TransactionType.Expense || !cardIds.Contains(trx.CardId))
            {
                continue;
            }

            if (byCategory.TryGetValue(trx.Category, out var prev))
            {
                byCategory[trx.Category] = prev + trx.Amount;
            }
            else
            {
                byCategory[trx.Category] = trx.Amount;
            }
        }

        return byCategory;
    }

    public static string FormatMoney(decimal amount, Currency currency)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{amount:F2} {currency}");
    }
}
