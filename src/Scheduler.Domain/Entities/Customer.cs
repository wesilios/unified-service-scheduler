namespace Scheduler.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    private Customer()
    {
    }

    public Customer(Guid id, string name, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
    }
}
