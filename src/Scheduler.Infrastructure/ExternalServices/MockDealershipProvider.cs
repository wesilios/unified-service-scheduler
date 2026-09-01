using Scheduler.Application.Interfaces;
using Scheduler.Domain.Entities;

namespace Scheduler.Infrastructure.ExternalServices;

// Placeholder for this assessment — returns static mock data instead of calling the
// real internal Dealership service. See IDealershipHttpClient (Refit, unwired) for the
// future real implementation this would be swapped for. Same known-seed-id pattern as
// the previous EF `HasData` seed, so existing tests/Postman/README references still resolve.
public sealed class MockDealershipProvider : IDealershipProvider
{
    private static readonly Dealership DowntownDealership = new(
        id: new Guid("11111111-1111-1111-1111-111111111111"),
        name: "Downtown Dealership",
        operatingHoursStart: new TimeOnly(8, 0),
        operatingHoursEnd: new TimeOnly(17, 0));

    private static readonly Dictionary<Guid, Dealership> Dealerships = new()
    {
        [DowntownDealership.Id] = DowntownDealership
    };

    public Task<Dealership?> GetAsync(Guid dealershipId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Dealerships.GetValueOrDefault(dealershipId));
}
