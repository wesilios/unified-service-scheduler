namespace Scheduler.Domain.ValueObjects;

public sealed record ServiceType(string Code, string Description, TimeSpan Duration);
