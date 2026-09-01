using Scheduler.Application.Interfaces;
using Scheduler.Domain.Entities;
using Scheduler.Infrastructure.InternalClients;

namespace Scheduler.Infrastructure.InternalServices;

public class DealershipProvider : IDealershipProvider
{
    private readonly IDealershipHttpClient _client;

    public DealershipProvider(IDealershipHttpClient client)
    {
        _client = client;
    }

    public Task<Dealership?> GetAsync(Guid dealershipId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}