using System.Text.Json;
using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonDataStore
{
    // path to file (might be directory in edge situations)
    private readonly string _filePath;
    // serializer options define serialization options
    private readonly JsonSerializerOptions _options;

    public JsonDataStore(string filePath)
    {
        _filePath = filePath;
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public DataFile Load()
    {
        // if file is missing we load by creating it first and then loading empty from memory
        if (!File.Exists(_filePath))
        {
            var empty = new DataFile();
            Save(empty);
            return empty;
        }

        // read json text from file system as text
        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            var empty = new DataFile();
            Save(empty);
            return empty;
        }

        // deserialize and then normalize collections because null is not list
        var result = JsonSerializer.Deserialize<DataFile>(json, _options);
        if (result == null)
        {
            var empty = new DataFile();
            Save(empty);
            return empty;
        }

        result.Cards ??= new List<Card>();
        result.Transactions ??= new List<Transaction>();
        result.DailyLimits ??= new List<DailyLimit>();

        return result;
    }

    public void Save(DataFile data)
    {
        // create directory if path has directory, otherwise skip to avoid creating file
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // writing JSON string to file replaces existing content with new old content
        var json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(_filePath, json);
    }
}

public sealed class DataFile
{
    // cards are cards
    public List<Card> Cards { get; set; } = new();

    // transactions are card operations
    public List<Transaction> Transactions { get; set; } = new();

    // limits for day/week (currently day)
    public List<DailyLimit> DailyLimits { get; set; } = new();

    public DateOnly? LastCushionDeclinedDate { get; set; }

    public bool HasSeenOnboarding { get; set; }

    public Guid? DefaultCardId { get; set; }
}
