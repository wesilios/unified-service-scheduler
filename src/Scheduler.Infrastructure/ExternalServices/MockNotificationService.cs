using Scheduler.Application.Interfaces;
using Scheduler.Domain.Entities;

namespace Scheduler.Infrastructure.ExternalServices;

// Placeholder for this assessment — a real implementation would call the external
// Notification system. Booking correctness never depends on this succeeding (see
// architecture.md Reliability §10 — notification is best-effort, not transactional).
public sealed class MockNotificationService : INotificationService
{
    public Task SendConfirmationAsync(Appointment appointment, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
