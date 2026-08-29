using Scheduler.Domain.Entities;

namespace Scheduler.Application.Interfaces;

public interface INotificationService
{
    Task SendConfirmationAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
