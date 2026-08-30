using Microsoft.EntityFrameworkCore;
using Scheduler.Application.Interfaces;
using Scheduler.Domain.Entities;
using Scheduler.Domain.Exceptions;

namespace Scheduler.Infrastructure.DataAccess;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly SchedulerDbContext _db;

    public CustomerRepository(SchedulerDbContext db)
    {
        _db = db;
    }

    public Task<Customer?> FindByEmailAndPhoneAsync(string email, string phone, CancellationToken cancellationToken = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Email == email && c.Phone == phone, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _db.Customers.Add(customer);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Lost the race on UNIQUE(Email, Phone) to a concurrent guest-checkout request.
            throw new CustomerConflictException("A customer with this email and phone already exists.")
            {
                Data = { ["InnerException"] = ex.Message }
            };
        }
    }
}
