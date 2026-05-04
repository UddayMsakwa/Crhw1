using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonCardRepository : ICardRepository
{
    private readonly JsonDataStore _store;

    public JsonCardRepository(JsonDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<Card> GetAll()
    {
        return _store.Load().Cards.OrderBy(c => c.Id).ToList();
    }

    public Card? GetById(int id)
    {
        return _store.Load().Cards.FirstOrDefault(c => c.Id == id);
    }

    public Card? GetDefault()
    {
        return _store.Load().Cards.FirstOrDefault(c => c.IsDefault);
    }

    public Card? GetDefaultByDataStore()
    {
        var data = _store.Load();
        if (!data.DefaultCardId.HasValue)
        {
            return null;
        }

        var id = GuidToCardId(data.DefaultCardId.Value);
        return data.Cards.FirstOrDefault(c => c.Id == id);
    }

    public Card? GetFirst()
    {
        return _store.Load().Cards.OrderBy(c => c.Id).FirstOrDefault();
    }

    public Card Add(Card card)
    {
        var data = _store.Load();
        card.Id = data.Cards.Count == 0 ? 1 : data.Cards.Max(c => c.Id) + 1;
        if (data.Cards.Count == 0)
        {
            card.IsDefault = true;
            data.DefaultCardId = CardIdToGuid(card.Id);
        }

        data.Cards.Add(card);
        _store.Save(data);
        return card;
    }

    public void SetDefault(int cardId)
    {
        var data = _store.Load();
        foreach (var card in data.Cards)
        {
            card.IsDefault = card.Id == cardId;
        }

        data.DefaultCardId = CardIdToGuid(cardId);

        _store.Save(data);
    }

    private static Guid CardIdToGuid(int cardId)
    {
        var raw = cardId.ToString("D12");
        return Guid.Parse($"00000000-0000-0000-0000-{raw}");
    }

    private static int GuidToCardId(Guid guid)
    {
        var raw = guid.ToString("N");
        var tail = raw.Substring(raw.Length - 12, 12);
        return int.TryParse(tail, out var result) ? result : -1;
    }
}
