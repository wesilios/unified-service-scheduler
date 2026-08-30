using Scheduler.Domain.Entities;

namespace Scheduler.Application.Interfaces;

public interface IDealershipRepository
{
    Task<Dealership?> GetAsync(Guid dealershipId, CancellationToken cancellationToken = default);
}
