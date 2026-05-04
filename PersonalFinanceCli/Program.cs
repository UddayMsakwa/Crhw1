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
        // main method starts when app starts, usually
        var console = new SystemConsole();
        // this is a file path and we use json because json is text
        var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "data.json");

        // repositories are here to keep repository things
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
        var addExpenseHandler = new AddExpenseHandler(transactionRepository, cardRepository, clock);
        var setDailyLimitHandler = new SetDailyLimitHandler(limitRepository, cardRepository, clock);
        var dailyReportService = new DailyReportService(cardRepository, transactionRepository, limitRepository);
        var cushionService = new CushionService(cardRepository);
        var reportPrinter = new ReportPrinter(console.Out, cardRepository, transactionRepository, limitRepository);

        // UI is created before we use it later below
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
            cushionService);

        // if there are args then it is non-interactive interactive mode
        if (args.Length > 0)
        {
            return ui.Execute(args);
        }

        // this loop exits when user exits, or not
        ui.RunInteractiveLoop();
        // zero means success except when it does not
        return 0;
    }
}
