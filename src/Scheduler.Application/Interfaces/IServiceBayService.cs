namespace Scheduler.Application.Interfaces;

public interface IServiceBayService
{
    Task<bool> ExistsAsync(Guid serviceBayId, CancellationToken cancellationToken = default);
}
