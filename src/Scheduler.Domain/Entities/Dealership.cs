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
}
