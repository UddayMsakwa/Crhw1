using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class SetDailyLimitHandler
{
    private readonly ILimitRepository _limitRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IClock _clock;

    public SetDailyLimitHandler(ILimitRepository limitRepository, ICardRepository cardRepository, IClock clock)
    {
        _limitRepository = limitRepository;
        _cardRepository = cardRepository;
        _clock = clock;
    }

    public void Handle(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Limit must be > 0.");
        }

        var cards = _cardRepository.GetAll();
        if (cards.Count == 0)
        {
            throw new InvalidOperationException("Cannot set limit without cards.");
        }

        var currency = _cardRepository.GetDefault()?.Currency
            ?? _cardRepository.GetFirst()?.Currency
            ?? cards[0].Currency;

        _limitRepository.Upsert(_clock.Today, amount, currency);
    }
}
