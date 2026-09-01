namespace Scheduler.Domain.Entities;

// Value Object owned by Appointment (EF Core OwnsOne) — no identity of its own, no table.
// Two Appointments from the same person simply carry two independent copies of the same
// Name/Email/Phone values; that's expected, not a data-integrity concern to dedupe against.
public class Customer
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    private Customer()
    {
    }

    public Customer(string name, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        Name = name;
        Email = email;
        Phone = phone;
    }
}
