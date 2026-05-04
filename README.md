# PersonalFinanceCli

Console app for home budgeting on .NET 8 (`net8.0`).

## Install .NET 8

1. Download and install the SDK: https://dotnet.microsoft.com/download/dotnet/8.0
2. Verify installation:

```bash
dotnet --version
```

It should print version `8.x`.

## Solution structure

- `PersonalFinanceCli` - main application
- `PersonalFinanceCli.Tests` - xUnit tests + coverage via `coverlet.collector`

## Build

```bash
dotnet build
```

## Tests and coverage

```bash
dotnet test
```

Coverage result (Cobertura) is written to `TestResults/.../coverage.cobertura.xml`.

## Run

### Interactive mode (REPL)

```bash
dotnet run --project PersonalFinanceCli
```

After start, prompt `> ` is shown and the app keeps running after each command.

Supported commands:

- `help` - list of commands and examples
- `exit` - leave REPL
- `card add`, `card set-default`, `expense add`, `income add`, `limit set` - print daily report after execution
- `report day [--date ...]` - prints report and returns to REPL
- `limit show`, `card list` - print result and return to REPL

### Wizard mode for incomplete commands

If required parameters are missing, the app asks questions and completes the command.

Question examples:

- `Amount?`
- `Category?`
- `Card? (enter to use default)`
- `Date? (YYYY-MM-DD, enter = today)`
- `Currency (RUB/EUR)?`
- `Daily limit amount?`

At any step, enter `cancel` to abort the operation.
If number/date/currency format is invalid, the same question is asked again.

### One-off run via arguments

```bash
dotnet run --project PersonalFinanceCli -- <args>
```

One-off examples:

```bash
dotnet run --project PersonalFinanceCli -- card add "Tinkoff" RUB 1000
dotnet run --project PersonalFinanceCli -- card add "Cash" RUB 200
dotnet run --project PersonalFinanceCli -- card list
dotnet run --project PersonalFinanceCli -- card set-default 2

dotnet run --project PersonalFinanceCli -- income add 1500 "Salary"
dotnet run --project PersonalFinanceCli -- expense add 12.5 "Food" --note "Lunch"
dotnet run --project PersonalFinanceCli -- expense add 3.4 "Coffee" --date 2026-03-01

dotnet run --project PersonalFinanceCli -- limit set 1000
dotnet run --project PersonalFinanceCli -- limit show

dotnet run --project PersonalFinanceCli -- report day
dotnet run --project PersonalFinanceCli -- report day --date 2026-03-01
```

## Data storage

Data is stored in `data.json` in the current working directory.

Format:

```json
{
  "cards": [],
  "transactions": [],
  "dailyLimits": []
}
```
