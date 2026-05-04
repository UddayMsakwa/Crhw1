namespace PersonalFinanceCli.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, Currency Currency)
{
    public override string ToString()
    {
        return $"{Amount:F2} {Currency}";
    }
}
