using Scheduler.Domain.Entities;

namespace Scheduler.Application.Interfaces;

public interface IDealershipProvider
{
    Task<Dealership?> GetAsync(Guid dealershipId, CancellationToken cancellationToken = default);
}
