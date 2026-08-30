using Scheduler.Application.Queries;
using Scheduler.Application.Services;

namespace Scheduler.Application.Handlers;

public sealed class CheckAvailabilityQueryHandler : IQueryHandler<CheckAvailabilityQuery, AvailabilityResult>
{
    private readonly AppointmentAvailabilityChecker _checker;

    public CheckAvailabilityQueryHandler(AppointmentAvailabilityChecker checker)
    {
        _checker = checker;
    }

    public async Task<AvailabilityResult> HandleAsync(CheckAvailabilityQuery query)
    {
        var outcome = await _checker.CheckAsync(
            query.DealershipId,
            query.TechnicianId,
            query.ServiceBayId,
            query.ServiceTypeCode,
            query.StartTime);

        return new AvailabilityResult(outcome.Status, outcome.Reason);
    }
}
