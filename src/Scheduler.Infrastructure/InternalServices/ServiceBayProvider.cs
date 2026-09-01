using Scheduler.Application.Interfaces;
using Scheduler.Infrastructure.InternalClients;

namespace Scheduler.Infrastructure.InternalServices;

public class ServiceBayProvider : IServiceBayProvider
{
    private readonly IServiceBayHttpClient _client;

    public ServiceBayProvider(IServiceBayHttpClient client)
    {
        _client = client;
    }

    public Task<bool> ExistsAsync(Guid serviceBayId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
