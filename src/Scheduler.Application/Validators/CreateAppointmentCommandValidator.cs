using FluentValidation;
using Scheduler.Application.Commands;

namespace Scheduler.Application.Validators;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty();
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.CustomerPhone).NotEmpty();
        RuleFor(x => x.DealershipId).NotEmpty();
        RuleFor(x => x.TechnicianId).NotEmpty();
        RuleFor(x => x.ServiceBayId).NotEmpty();
        RuleFor(x => x.ServiceTypeCode).NotEmpty();
        RuleFor(x => x.Vehicle).NotEmpty().WithMessage("Vehicle description must not be empty.");
        RuleFor(x => x.StartTime).GreaterThan(DateTime.UtcNow).WithMessage("StartTime must be in the future.");
    }
}
