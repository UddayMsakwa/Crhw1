namespace PersonalFinanceCli.Infrastructure.Time;

public interface IClock
{
    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

public sealed class FakeClock : IClock
{
    public FakeClock(DateOnly today)
    {
        Today = today;
    }

    public DateOnly Today { get; set; }
}
