using PersonalFinanceCli.Application.CommandHandlers;
using PersonalFinanceCli.Application.Services;
using PersonalFinanceCli.Domain.Services;
using PersonalFinanceCli.Infrastructure.Persistence;
using PersonalFinanceCli.Infrastructure.Time;
using PersonalFinanceCli.Presentation.Parsing;
using PersonalFinanceCli.Presentation.Rendering;

namespace PersonalFinanceCli.Tests;

internal sealed class TestAppContext : IDisposable
{
    private readonly string _tempDirectory;
    private readonly bool _ownsDirectory;

    public TestAppContext(
        DateOnly today,
        IEnumerable<string?>? inputLines = null,
        string? existingDirectory = null,
        bool keepDirectory = false)
    {
        _tempDirectory = existingDirectory ?? Path.Combine(Path.GetTempPath(), "pfcli-tests", Guid.NewGuid().ToString("N"));
        _ownsDirectory = existingDirectory is null || !keepDirectory;
        Directory.CreateDirectory(_tempDirectory);

        var dataPath = Path.Combine(_tempDirectory, "data.json");
        Store = new JsonDataStore(dataPath);
        CardRepository = new JsonCardRepository(Store);
        TransactionRepository = new JsonTransactionRepository(Store);
        LimitRepository = new JsonLimitRepository(Store);
        OnboardingStateRepository = new JsonOnboardingStateRepository(Store);
        Clock = new FakeClock(today);

        Console = new FakeConsole(inputLines ?? Array.Empty<string?>());

        var parser = new CommandParser();
        var addCardHandler = new AddCardHandler(CardRepository);
        var setDefaultCardHandler = new SetDefaultCardHandler(CardRepository);
        var addTransactionHandler = new AddTransactionHandler(TransactionRepository, CardRepository, Clock);
        var addIncomeHandler = new AddIncomeHandler(addTransactionHandler);
        var addExpenseHandler = new AddExpenseHandler(TransactionRepository, CardRepository, Clock);
        var setDailyLimitHandler = new SetDailyLimitHandler(LimitRepository, CardRepository, Clock);
        var dailyReportService = new DailyReportService(CardRepository, TransactionRepository, LimitRepository);
        var cushionService = new CushionService(CardRepository);
        var reportPrinter = new ReportPrinter(Console.Out, CardRepository, TransactionRepository, LimitRepository);

        Ui = new ConsoleUi(
            parser,
            addCardHandler,
            setDefaultCardHandler,
            addTransactionHandler,
            addIncomeHandler,
            addExpenseHandler,
            setDailyLimitHandler,
            dailyReportService,
            reportPrinter,
            CardRepository,
            LimitRepository,
            OnboardingStateRepository,
            Clock,
            Console,
            cushionService);
    }

    public JsonDataStore Store { get; }

    public JsonCardRepository CardRepository { get; }

    public JsonTransactionRepository TransactionRepository { get; }

    public JsonLimitRepository LimitRepository { get; }

    public JsonOnboardingStateRepository OnboardingStateRepository { get; }

    public FakeClock Clock { get; }

    public FakeConsole Console { get; }

    public ConsoleUi Ui { get; }

    public string DirectoryPath => _tempDirectory;

    public int Run(params string[] args)
    {
        Console.ClearOutput();
        return Ui.Execute(args);
    }

    public void RunInteractive()
    {
        Console.ClearOutput();
        Ui.RunInteractiveLoop();
    }

    public string Output => Console.Output;

    public void Dispose()
    {
        try
        {
            if (_ownsDirectory && Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // ignore test cleanup issues
        }
    }
}
