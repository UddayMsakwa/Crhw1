namespace PersonalFinanceCli.Presentation.Parsing.Commands;

public sealed record CardAddCommand(
    string Name,
    string Currency,
    decimal? InitialBalance) : ParsedCommand;

public sealed record CardListCommand : ParsedCommand;

public sealed record CardSetDefaultCommand(int CardId) : ParsedCommand;