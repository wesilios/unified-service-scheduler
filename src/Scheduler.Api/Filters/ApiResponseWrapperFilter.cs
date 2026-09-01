using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Scheduler.Api.Contracts;

namespace Scheduler.Api.Filters;

// Registered globally (Program.cs: AddControllers(options => options.Filters.Add<...>())) so
// every controller action's result gets wrapped in the ApiResponse envelope — this is the one
// place that translates "whatever the controller returned" (a domain entity, a
// ProblemDetails, a ValidationProblemDetails, an anonymous object) into the standard
// Data/StatusCode/Message/Errors shape. Controllers keep using ordinary ASP.NET Core result
// helpers (Ok, Created, Problem, ValidationProblem) — none of them need to know this envelope
// exists. Unhandled exceptions don't reach this filter (they short-circuit past the result
// pipeline into exception-handling middleware instead) — see ApiExceptionHandler for that
// path; both call the same ApiResponseFactory so the envelope shape is defined once.
public sealed class ApiResponseWrapperFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: not ApiResponse } objectResult)
        {
            var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;

            objectResult.Value = objectResult.Value switch
            {
                ValidationProblemDetails validationProblem => ApiResponseFactory.Failure(
                    statusCode,
                    "One or more validation errors occurred.",
                    MapValidationErrors(validationProblem)),
                ProblemDetails problem => ApiResponseFactory.Failure(statusCode, problem.Detail ?? problem.Title ?? "An error occurred.", MapProblemErrors(problem)),
                _ when statusCode is >= 200 and < 300 => ApiResponseFactory.Success(objectResult.Value, statusCode),
                _ => ApiResponseFactory.Failure(statusCode, "An error occurred.", Array.Empty<ApiError>())
            };
            objectResult.DeclaredType = typeof(ApiResponse);
        }

        return next();
    }

    private static IReadOnlyList<ApiError> MapValidationErrors(ValidationProblemDetails problem) =>
        problem.Errors
            .SelectMany(field => field.Value.Select(message => new ApiError(field.Key, message)))
            .ToList();

    // Business-rule failures reach Problem() with the machine-readable status name stashed in
    // Extensions["errorCode"] (see AppointmentsController) — Title/Detail stay human text.
    private static IReadOnlyList<ApiError> MapProblemErrors(ProblemDetails problem)
    {
        var errorCode = problem.Extensions.TryGetValue("errorCode", out var value) && value is not null
            ? value.ToString()!
            : problem.Status?.ToString() ?? "ERROR";

        return [new ApiError(errorCode, problem.Detail ?? problem.Title ?? "An error occurred.")];
    }
}
