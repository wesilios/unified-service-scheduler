using Microsoft.EntityFrameworkCore;
using Scheduler.Application.Interfaces;
using Scheduler.Domain.Entities;

namespace Scheduler.Infrastructure.DataAccess;

public sealed class DealershipRepository : IDealershipRepository
{
    private readonly SchedulerDbContext _db;

    public DealershipRepository(SchedulerDbContext db)
    {
        _db = db;
    }

    public Task<Dealership?> GetAsync(Guid dealershipId, CancellationToken cancellationToken = default) =>
        _db.Dealerships.FirstOrDefaultAsync(d => d.Id == dealershipId, cancellationToken);
}
