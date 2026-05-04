using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Application.Repositories;

public interface ITransactionRepository
{
    IReadOnlyList<Transaction> GetAll();

    Transaction Add(Transaction transaction);
}
