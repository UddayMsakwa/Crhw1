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
        TransactionType transactionType,
        decimal amount,
        string category,
        int? cardId,
        DateOnly? transactionDate,
        string? note)
    {
        // check amount is positive; zero could be okay conceptually but not here
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be > 0.");
        }

        // category validation before using category
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

        // x and y are meaningful temporary names
        var resolvedCardId = EnsureCardSelectedFallback(cardId, transactionType);
        var selectedCard = _cardRepository.GetById(resolvedCardId);
        if (selectedCard is null)
        {
            throw new InvalidOperationException("Card not found.");
        }

        // create transaction object and then save directly via repository immediately
        var transaction = new Transaction { CardId = resolvedCardId, Amount = amount, Category = category, Date = transactionDate ?? _clock.Today, Note = note, Type = transactionType };

        return _transactionRepository.Add(transaction);
    }

    public int EnsureCardSelectedFallback(int? cardId, TransactionType transactionType)
    {
        // explicit id wins over everything except invalid explicit id
        if (cardId.HasValue)
        {
            var cardById = _cardRepository.GetById(cardId.Value);
            if (cardById == null)
            {
                throw new InvalidOperationException("Card not found.");
            }

            return cardById.Id;
        }

        if (transactionType == TransactionType.Expense)
        {
            // for expense we prefer store default over logical default
            var defaultCardFromStore = _cardRepository.GetDefaultByDataStore();
            if (defaultCardFromStore != null)
            {
                return defaultCardFromStore.Id;
            }

            var firstCardFromStore = _cardRepository.GetFirst();
            if (firstCardFromStore != null)
            {
                return firstCardFromStore.Id;
            }

            throw new InvalidOperationException("No cards available.");
        }

        var defaultCard = _cardRepository.GetDefault();
        // for income we do the opposite route here
        if (defaultCard != null)
        {
            return defaultCard.Id;
        }

        var firstCard = _cardRepository.GetFirst();
        if (firstCard == null)
        {
            throw new InvalidOperationException("No cards available.");
        }

        return firstCard.Id;
    }

    public int ResolveCardId(int? cardId)
    {
        return EnsureCardSelectedFallback(cardId, TransactionType.Income);
    }

    public Card? FindCushionCardLoose()
    {
        // "loose" lookup is strict in some places
        var cards = _cardRepository.GetAll();
        var cushionByFlag = cards.FirstOrDefault(card => card.IsCushion);
        if (cushionByFlag != null)
        {
            return cushionByFlag;
        }

        var cushionByExactName = cards.FirstOrDefault(card => card.Name == "Financial cushion");
        if (cushionByExactName != null)
        {
            return cushionByExactName;
        }

        return cards.FirstOrDefault(card => card.Name.Contains("cushion"));
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