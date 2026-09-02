using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Scheduler.Api.Contracts;
using Scheduler.Application.Commands;
using Scheduler.Application.Interfaces;
using Scheduler.Application.Queries;
using Scheduler.Application.Results;
using Scheduler.Domain.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AppointmentsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // Binds the Presentation-layer Request DTO, not the Application-layer Command —
    // CreateAppointmentCommand never appears in the OpenAPI/Scalar documentation.
    // ProducesResponseType is required here (not just for documentation completeness) because
    // this action returns IActionResult — without it the OpenAPI generator has no static
    // return type to reflect over and emits no response schema/example at all. The declared
    // types are ApiResponseOf<T>, not the raw payload, so the generated schema matches what
    // ApiResponseWrapperFilter actually puts on the wire.
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseOf<Appointment>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseOf<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseOf<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentRequest request,
        [FromServices] IValidator<CreateAppointmentCommand> validator)
    {
        var command = request.ToCommand();

        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = (AppointmentResult)await _dispatcher.SendAsync(command);

        // ErrorCode goes in Extensions, not Title — ApiResponseWrapperFilter reads it from
        // there to populate the ApiResponse envelope's Errors[].ErrorCode. Title/Detail stay
        // human text; Extensions carries the machine-readable part.
        return result.Status switch
        {
            AppointmentResultStatus.Success => Created($"/appointments/{result.Appointment!.Id}", result.Appointment),
            AppointmentResultStatus.Conflict => Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["errorCode"] = result.Status.ToString() }),
            _ => Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["errorCode"] = result.Status.ToString() })
        };
    }

    // Binds the query string to a single Request DTO instead of five separate
    // [FromQuery] parameters; CheckAvailabilityQuery stays internal to the Application
    // layer and never appears in the OpenAPI/Scalar documentation.
    [HttpGet("availability")]
    [ProducesResponseType(typeof(ApiResponseOf<AvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseOf<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckAvailability(
        [FromQuery] CheckAvailabilityRequest request,
        [FromServices] IValidator<CheckAvailabilityQuery> validator)
    {
        var query = request.ToQuery();

        var validation = await validator.ValidateAsync(query);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _dispatcher.QueryAsync<CheckAvailabilityQuery, AvailabilityResult>(query);

        return Ok(new AvailabilityResponse(
            result.Status == AvailabilityStatus.Available,
            result.Status.ToString(),
            result.Reason));
    }
}
