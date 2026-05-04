using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Presentation.Parsing.Commands;

public sealed record TransactionAddCommand(
    TransactionType Type,
    decimal Amount,
    string Category,
    int? CardId,
    DateOnly? Date,
    string? Note) : ParsedCommand;