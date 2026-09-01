using Scheduler.Application.Interfaces;

namespace Scheduler.Infrastructure.InternalServices;

// Placeholder for this assessment — returns static mock data instead of calling the
// real internal Service Bay service. See IServiceBayHttpClient (Refit, unwired) for the
// future real implementation this would be swapped for. Guid.Empty is treated as "not
// found" so the invalid-resource branch (Data Flow) is exercisable; any other id is
// treated as a valid service bay.
public sealed class MockServiceBayProvider : IServiceBayProvider
{
    public Task<bool> ExistsAsync(Guid serviceBayId, CancellationToken cancellationToken = default) =>
        Task.FromResult(serviceBayId != Guid.Empty);
}
