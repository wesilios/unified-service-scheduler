namespace Scheduler.Domain.ValueObjects;

public sealed class TimeRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public TimeRange(DateTime start, DateTime end)
    {
        if (end <= start)
        {
            throw new ArgumentException("End must be after Start.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public bool Overlaps(TimeRange other) => Start < other.End && other.Start < End;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
