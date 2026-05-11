using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.Services;

public sealed class CardResolver
{
    private readonly ICardRepository _cardRepository;

    public CardResolver(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    public int ResolveCardIdForTransaction(int? cardId, TransactionType transactionType)
    {
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
}