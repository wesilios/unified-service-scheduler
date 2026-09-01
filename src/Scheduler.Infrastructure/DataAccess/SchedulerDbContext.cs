using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Scheduler.Domain.Entities;

namespace Scheduler.Infrastructure.DataAccess;

public class SchedulerDbContext : DbContext
{
    private IDbContextTransaction? _currentTransaction;

    public SchedulerDbContext(DbContextOptions<SchedulerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<Customer> Customers => Set<Customer>();

    // Each entity's Fluent API configuration lives in its own IEntityTypeConfiguration<T>
    // class under DataAccess/Configurations/ — keeps this file small as the model grows;
    // add a new entity's configuration there and it's picked up automatically below.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulerDbContext).Assembly);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        return _currentTransaction == null ? Task.CompletedTask : _currentTransaction.RollbackAsync(cancellationToken);
    }
}