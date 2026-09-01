using Scheduler.Application.Interfaces;
using Scheduler.Application.Queries;

namespace Scheduler.Application.Handlers;

public sealed class CheckAvailabilityQueryHandler : IQueryHandler<CheckAvailabilityQuery, AvailabilityResult>
{
    private readonly IAppointmentAvailabilityChecker _checker;

    public CheckAvailabilityQueryHandler(IAppointmentAvailabilityChecker checker)
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
