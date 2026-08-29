using Scheduler.Domain;
using Scheduler.Domain.Entities;

namespace Scheduler.Application.Interfaces;

public interface IAppointmentRepository
{
    Task<IReadOnlyList<Appointment>> GetOverlappingAsync(
        Guid technicianId,
        Guid serviceBayId,
        TimeRange range,
        CancellationToken cancellationToken = default);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
