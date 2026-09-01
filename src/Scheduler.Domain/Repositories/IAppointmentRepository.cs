using Scheduler.Domain.Entities;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.Domain.Repositories;

public interface IAppointmentRepository
{
    Task<IReadOnlyList<Appointment>> GetOverlappingAsync(
        Guid technicianId,
        Guid serviceBayId,
        TimeRange range,
        CancellationToken cancellationToken = default);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
