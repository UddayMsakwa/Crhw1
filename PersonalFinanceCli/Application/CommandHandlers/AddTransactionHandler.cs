using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Application.Services;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddTransactionHandler
{
    public const string TransferToCushion = "Transfer to cushion";
    public const string TransferFromIncome = "Transfer from income";

    private readonly ITransactionRepository _transactionRepository;
    private readonly ICardRepository _cardRepository;
    private readonly CardResolver _cardResolver;
    private readonly IClock _clock;

    public AddTransactionHandler(
        ITransactionRepository transactionRepository,
        ICardRepository cardRepository,
        IClock clock)
    {
        _transactionRepository = transactionRepository;
        _cardRepository = cardRepository;
        _cardResolver = new CardResolver(cardRepository);
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
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be > 0.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

        var resolvedCardId = _cardResolver.ResolveCardIdForTransaction(cardId, transactionType);
        var selectedCard = _cardRepository.GetById(resolvedCardId);
        if (selectedCard is null)
        {
            throw new InvalidOperationException("Card not found.");
        }

        var transaction = new Transaction
        {
            CardId = resolvedCardId,
            Amount = amount,
            Category = category,
            Date = transactionDate ?? _clock.Today,
            Note = note,
            Type = transactionType
        };

        return _transactionRepository.Add(transaction);
    }

    public int ResolveCardId(int? cardId)
    {
        return _cardResolver.ResolveCardIdForTransaction(cardId, TransactionType.Income);
    }

    public Card? FindCushionCardLoose()
    {
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