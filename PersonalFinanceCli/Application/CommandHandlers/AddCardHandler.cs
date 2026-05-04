using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddCardHandler
{
    private readonly ICardRepository _cardRepository;

    public AddCardHandler(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    public Card Handle(string name, string currencyRaw, decimal? initialBalance)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Card name cannot be empty.");
        }

        if (!Enum.TryParse<Currency>(currencyRaw, true, out var currency))
        {
            throw new InvalidOperationException("Unknown currency. Allowed: RUB, EUR.");
        }

        var card = new Card
        {
            Name = name,
            Currency = currency,
            InitialBalance = initialBalance ?? 0m,
            IsDefault = _cardRepository.GetAll().Count == 0
        };

        return _cardRepository.Add(card);
    }
}
