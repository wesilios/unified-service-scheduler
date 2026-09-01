using Scheduler.Domain.ValueObjects;

namespace Scheduler.Domain.Entities;

public class Dealership
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TimeOnly OperatingHoursStart { get; private set; }
    public TimeOnly OperatingHoursEnd { get; private set; }

    private Dealership()
    {
    }

    public Dealership(Guid id, string name, TimeOnly operatingHoursStart, TimeOnly operatingHoursEnd)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        Id = id;
        Name = name;
        OperatingHoursStart = operatingHoursStart;
        OperatingHoursEnd = operatingHoursEnd;
    }

    // Moved from AppointmentSchedulingPolicy: operating hours is this type's own invariant —
    // once Dealership is owned by its own bounded context, Scheduler asks the fetched copy
    // "am I within your hours" rather than reaching into its fields itself. See architecture.md
    // §3 Dealership and Architecture Principle #8.
    public bool IsWithinOperatingHours(TimeRange range)
    {
        if (range.Start.DayOfWeek == DayOfWeek.Sunday || range.End.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        if (range.Start.Date != range.End.Date)
        {
            return false;
        }

        var startTime = TimeOnly.FromDateTime(range.Start);
        var endTime = TimeOnly.FromDateTime(range.End);

        return startTime >= OperatingHoursStart && endTime <= OperatingHoursEnd;
    }
}
