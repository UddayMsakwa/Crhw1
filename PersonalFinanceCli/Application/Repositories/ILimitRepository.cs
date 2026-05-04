using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Application.Repositories;

public interface ILimitRepository
{
    DailyLimit? GetByDate(DateOnly date);

    DailyLimit Upsert(DateOnly date, decimal amount, Domain.ValueObjects.Currency currency);
}
