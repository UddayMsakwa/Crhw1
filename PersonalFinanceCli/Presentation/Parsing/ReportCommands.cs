namespace PersonalFinanceCli.Presentation.Parsing.Commands;

public sealed record ReportDayCommand(DateOnly? Date) : ParsedCommand;