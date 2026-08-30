namespace Scheduler.Domain.Exceptions;

// Thrown by ICustomerRepository.AddAsync when the Customer UNIQUE(Email, Phone) constraint
// rejects the insert — i.e. two concurrent guest-checkout requests from the same customer
// both failed to find an existing record and both tried to create one. Unlike
// AppointmentConflictException, this is not surfaced to the caller as a failure: the
// handler catches it and re-queries, since the customer now provably exists.
public sealed class CustomerConflictException : Exception
{
    public CustomerConflictException(string message) : base(message)
    {
    }
}
