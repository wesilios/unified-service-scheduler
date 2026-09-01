using System.Threading;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Scheduler.Api.Contracts;

namespace Scheduler.Api.Middleware;

// Handles anything ApiResponseWrapperFilter never sees — an unhandled exception short-circuits
// past the MVC result pipeline straight into ASP.NET Core's exception-handling middleware
// (app.UseExceptionHandler() in Program.cs). This is the other half of "one place builds the
// envelope": both this class and ApiResponseWrapperFilter call ApiResponseFactory rather than
// constructing an ApiResponse by hand.
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var response = ApiResponseFactory.Failure(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            [new ApiError("INTERNAL_SERVER_ERROR", "An unexpected error occurred.")]);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
