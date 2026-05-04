using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Domain.Entities;

public sealed class Card
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Currency Currency { get; set; }

    public decimal InitialBalance { get; set; }

    public bool IsDefault { get; set; }

    public bool IsCushion { get; set; }
}
