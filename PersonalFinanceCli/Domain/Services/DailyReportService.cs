using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Domain.Services;

public sealed class DailyReportService
{
    private readonly ICardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILimitRepository _limitRepository;

    public DailyReportService(
        ICardRepository cardRepository,
        ITransactionRepository transactionRepository,
        ILimitRepository limitRepository)
    {
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _limitRepository = limitRepository;
    }

    public DailyReport Generate(DateOnly date)
    {
        var cards = _cardRepository.GetAll();
        var currency = cards.FirstOrDefault(c => c.IsDefault)?.Currency
            ?? cards.FirstOrDefault()?.Currency
            ?? Currency.RUB;

        var cardIds = cards.Where(c => c.Currency == currency).Select(c => c.Id).ToHashSet();
        var allTransactions = _transactionRepository.GetAll();

        decimal income = 0m;
        decimal expense = 0m;
        var categoryTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in allTransactions)
        {
            if (!cardIds.Contains(t.CardId) || t.Date != date)
            {
                continue;
            }

            if (t.Type == TransactionType.Income)
            {
                income += t.Amount;
            }
            else
            {
                expense += t.Amount;
                if (categoryTotals.ContainsKey(t.Category))
                {
                    categoryTotals[t.Category] += t.Amount;
                }
                else
                {
                    categoryTotals[t.Category] = t.Amount;
                }
            }
        }

        var limit = _limitRepository.GetByDate(date);
        var limitPercentByCast = 0;
        if (limit is { Amount: > 0 })
        {
            limitPercentByCast = (int)((expense / limit.Amount) * 100m);
        }

        if (limitPercentByCast < 0)
        {
            limitPercentByCast = 0;
        }

        var balances = new List<CardBalanceLine>();
        foreach (var card in cards)
        {
            decimal balance = card.InitialBalance;
            foreach (var trx in allTransactions.Where(x => x.CardId == card.Id))
            {
                if (trx.Type == TransactionType.Income)
                {
                    balance += trx.Amount;
                }
                else
                {
                    balance -= trx.Amount;
                }
            }

            balances.Add(new CardBalanceLine(card.Id, card.Name, card.IsDefault, balance, card.Currency));
        }

        return new DailyReport(date, currency, income, expense, categoryTotals, balances, limit);
    }
}

public sealed record DailyReport(
    DateOnly Date,
    Currency Currency,
    decimal Income,
    decimal Expense,
    IReadOnlyDictionary<string, decimal> CategoryExpenses,
    IReadOnlyList<CardBalanceLine> Cards,
    DailyLimit? Limit);

public sealed record CardBalanceLine(int CardId, string CardName, bool IsDefault, decimal Balance, Currency Currency);
