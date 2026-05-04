using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddIncomeHandler
{
    private readonly AddTransactionHandler _addTransactionHandler;

    public AddIncomeHandler(AddTransactionHandler addTransactionHandler)
    {
        _addTransactionHandler = addTransactionHandler;
    }

    public Transaction Handle(decimal amount, string category, int? cardId, DateOnly? date, string? note)
    {
        return _addTransactionHandler.Handle(TransactionType.Income, amount, category, cardId, date, note);
    }
}
