using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Domain.Entities;

public sealed class Transaction
{
    public int Id { get; set; }

    public int CardId { get; set; }

    public decimal Amount { get; set; }

    public string Category { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string? Note { get; set; }

    public TransactionType Type { get; set; }
}
