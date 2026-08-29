using Scheduler.Domain.Entities;

namespace Scheduler.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> FindByEmailAndPhoneAsync(string email, string phone, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
