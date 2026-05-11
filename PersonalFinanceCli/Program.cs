using PersonalFinanceCli.Application.CommandHandlers;
using PersonalFinanceCli.Application.Services;
using PersonalFinanceCli.Domain.Services;
using PersonalFinanceCli.Infrastructure.Persistence;
using PersonalFinanceCli.Infrastructure.Time;
using PersonalFinanceCli.Presentation.Parsing;
using PersonalFinanceCli.Presentation.Rendering;

namespace PersonalFinanceCli;

public static class Program
{
    public static int Main(string[] args)
    {
        var console = new SystemConsole();

        var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "data.json");

        var store = new JsonDataStore(dataPath);
        var cardRepository = new JsonCardRepository(store);
        var transactionRepository = new JsonTransactionRepository(store);
        var limitRepository = new JsonLimitRepository(store);
        var onboardingStateRepository = new JsonOnboardingStateRepository(store);
        var clock = new SystemClock();

        var parser = new CommandParser();
        var addCardHandler = new AddCardHandler(cardRepository);
        var setDefaultCardHandler = new SetDefaultCardHandler(cardRepository);

        var addTransactionHandler = new AddTransactionHandler(transactionRepository, cardRepository, clock);
        var addIncomeHandler = new AddIncomeHandler(addTransactionHandler);
        var addExpenseHandler = new AddExpenseHandler(addTransactionHandler);

        var setDailyLimitHandler = new SetDailyLimitHandler(limitRepository, cardRepository, clock);
        var dailyReportService = new DailyReportService(cardRepository, transactionRepository, limitRepository);
        var cushionService = new CushionService(cardRepository);
        var reportPrinter = new ReportPrinter(console.Out, cardRepository, transactionRepository, limitRepository);
        var wizardOptionCollector = new WizardOptionCollector();

        var ui = new ConsoleUi(
            parser,
            addCardHandler,
            setDefaultCardHandler,
            addTransactionHandler,
            addIncomeHandler,
            addExpenseHandler,
            setDailyLimitHandler,
            dailyReportService,
            reportPrinter,
            cardRepository,
            limitRepository,
            onboardingStateRepository,
            clock,
            console,
            cushionService,
            wizardOptionCollector);

        if (args.Length > 0)
        {
            return ui.Execute(args);
        }

        ui.RunInteractiveLoop();

        return 0;
    }
}