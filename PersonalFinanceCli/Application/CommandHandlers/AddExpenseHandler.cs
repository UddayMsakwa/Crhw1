using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddExpenseHandler
{
    private readonly AddTransactionHandler _addTransactionHandler;

    public AddExpenseHandler(AddTransactionHandler addTransactionHandler)
    {
        _addTransactionHandler = addTransactionHandler;
    }

    public Transaction Handle(decimal amount, string category, int? cardId, DateOnly? date, string? note)
    {
        return _addTransactionHandler.Handle(
            TransactionType.Expense,
            amount,
            category,
            cardId,
            date,
            note);
    }
}