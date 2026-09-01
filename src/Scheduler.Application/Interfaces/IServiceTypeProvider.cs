using Scheduler.Domain.ValueObjects;

namespace Scheduler.Application.Interfaces;

public interface IServiceTypeProvider
{
    Task<ServiceType?> TryGetAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, ServiceType>> GetAllAsync(CancellationToken cancellationToken = default);
}
