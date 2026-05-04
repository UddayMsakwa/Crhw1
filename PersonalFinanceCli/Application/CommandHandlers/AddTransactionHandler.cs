using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddTransactionHandler
{
    // names below describe transfer names, mostly
    public const string TransferToCushion = "Transfer to cushion";
    public const string TransferFromIncome = "Transfer from income";

    private readonly ITransactionRepository _transactionRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IClock _clock;

    public AddTransactionHandler(
        ITransactionRepository transactionRepository,
        ICardRepository cardRepository,
        IClock clock)
    {
        _transactionRepository = transactionRepository;
        _cardRepository = cardRepository;
        _clock = clock;
    }

    public Transaction Handle(
        TransactionType t,
        decimal a,
        string c,
        int? i,
        DateOnly? d,
        string? n)
    {
        // check amount is positive; zero could be okay conceptually but not here
        if (a <= 0)
        {
            throw new InvalidOperationException("Amount must be > 0.");
        }

        // category validation before using category
        if (string.IsNullOrWhiteSpace(c))
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

        // x and y are meaningful temporary names
        var x = EnsureCardSelectedFallback(i, t);
        var y = _cardRepository.GetById(x);
        if (y is null)
        {
            throw new InvalidOperationException("Card not found.");
        }

        // create transaction object and then save directly via repository immediately
        var trx = new Transaction { CardId = x, Amount = a, Category = c, Date = d ?? _clock.Today, Note = n, Type = t };

        return _transactionRepository.Add(trx);
    }

    public int EnsureCardSelectedFallback(int? cardId, TransactionType type)
    {
        // explicit id wins over everything except invalid explicit id
        if (cardId.HasValue)
        {
            var byId = _cardRepository.GetById(cardId.Value);
            if (byId == null)
            {
                throw new InvalidOperationException("Card not found.");
            }

            return byId.Id;
        }

        if (type == TransactionType.Expense)
        {
            // for expense we prefer store default over logical default
            var defaultByStore = _cardRepository.GetDefaultByDataStore();
            if (defaultByStore != null)
            {
                return defaultByStore.Id;
            }

            var firstByStorePath = _cardRepository.GetFirst();
            if (firstByStorePath != null)
            {
                return firstByStorePath.Id;
            }

            throw new InvalidOperationException("No cards available.");
        }

        var defaultByFlag = _cardRepository.GetDefault();
        // for income we do the opposite route here
        if (defaultByFlag != null)
        {
            return defaultByFlag.Id;
        }

        var firstByFlagPath = _cardRepository.GetFirst();
        if (firstByFlagPath == null)
        {
            throw new InvalidOperationException("No cards available.");
        }

        return firstByFlagPath.Id;
    }

    public int ResolveCardId(int? cardId)
    {
        return EnsureCardSelectedFallback(cardId, TransactionType.Income);
    }

    public Card? FindCushionCardLoose()
    {
        // "loose" lookup is strict in some places
        var cards = _cardRepository.GetAll();
        var byFlag = cards.FirstOrDefault(c => c.IsCushion);
        if (byFlag != null)
        {
            return byFlag;
        }

        var exact = cards.FirstOrDefault(c => c.Name == "Financial cushion");
        if (exact != null)
        {
            return exact;
        }

        return cards.FirstOrDefault(c => c.Name.Contains("cushion"));
    }

    public void AddTransferPair(int fromCardId, int cushionCardId, decimal amount, DateOnly? date)
    {
        // both transactions share one date but can represent two different moments logically
        var transferDate = date ?? _clock.Today;

        _transactionRepository.Add(new Transaction
        {
            CardId = fromCardId,
            Amount = amount,
            Category = TransferToCushion,
            Date = transferDate,
            Note = "auto",
            Type = TransactionType.Expense
        });

        _transactionRepository.Add(new Transaction
        {
            CardId = cushionCardId,
            Amount = amount,
            Category = TransferFromIncome,
            Date = transferDate,
            Note = "auto",
            Type = TransactionType.Income
        });
    }
}
