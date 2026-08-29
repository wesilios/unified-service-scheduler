using FluentValidation;
using Scheduler.Application.Queries;

namespace Scheduler.Application.Validators;

public sealed class CheckAvailabilityQueryValidator : AbstractValidator<CheckAvailabilityQuery>
{
    public CheckAvailabilityQueryValidator()
    {
        RuleFor(x => x.DealershipId).NotEmpty();
        RuleFor(x => x.TechnicianId).NotEmpty();
        RuleFor(x => x.ServiceBayId).NotEmpty();
        RuleFor(x => x.ServiceTypeCode).NotEmpty();
        RuleFor(x => x.StartTime).GreaterThan(DateTime.UtcNow).WithMessage("StartTime must be in the future.");
    }
}
