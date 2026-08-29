namespace Scheduler.Domain;

public sealed record ServiceType(string Code, string Description, TimeSpan Duration);
