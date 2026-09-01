using Microsoft.EntityFrameworkCore;
using Scheduler.Domain.Entities;
using Scheduler.Domain.Exceptions;
using Scheduler.Domain.Repositories;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.Infrastructure.DataAccess;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly SchedulerDbContext _db;

    public AppointmentRepository(SchedulerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Appointment>> GetOverlappingAsync(
        Guid technicianId,
        Guid serviceBayId,
        TimeRange range,
        CancellationToken cancellationToken = default)
    {
        // Fast-fail read-check only — not the concurrency guarantee. See Data Flow.
        return await _db.Appointments
            .Where(a => a.TechnicianId == technicianId || a.ServiceBayId == serviceBayId)
            .Where(a => a.Duration.Start < range.End && range.Start < a.Duration.End)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Any failure here (after the read-check already passed) is treated as a lost
            // race on the AppointmentSlot unique constraint — the real concurrency guarantee.
            throw new AppointmentConflictException(
                "Technician or Service Bay is already booked for the requested time.")
            {
                Data = { ["InnerException"] = ex.Message }
            };
        }
    }
}
