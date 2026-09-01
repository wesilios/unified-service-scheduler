namespace Scheduler.Application.Interfaces;

public interface IServiceBayProvider
{
    Task<bool> ExistsAsync(Guid serviceBayId, CancellationToken cancellationToken = default);
}
