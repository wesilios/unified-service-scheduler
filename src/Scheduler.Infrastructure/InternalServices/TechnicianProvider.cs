using Scheduler.Application.Interfaces;
using Scheduler.Infrastructure.InternalClients;

namespace Scheduler.Infrastructure.InternalServices;

public class TechnicianProvider : ITechnicianProvider
{
    private readonly ITechnicianHttpClient _client;

    public TechnicianProvider(ITechnicianHttpClient client)
    {
        _client = client;
    }

    public Task<bool> ExistsAsync(Guid technicianId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
