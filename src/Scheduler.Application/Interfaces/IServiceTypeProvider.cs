using Scheduler.Domain;

namespace Scheduler.Application.Interfaces;

public interface IServiceTypeProvider
{
    ServiceType? TryGet(string code);

    IReadOnlyDictionary<string, ServiceType> GetAll();
}
