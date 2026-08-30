namespace Scheduler.Api.Contracts;

// The one place that knows how to build an ApiResponse. ApiResponseWrapperFilter calls this
// for every controller result (success or failure); ApiExceptionHandler calls it for
// unhandled exceptions. Neither duplicates the shape — both just supply what varies
// (status code, message, data, errors) and let this factory assemble the envelope.
public static class ApiResponseFactory
{
    public static ApiResponse Success(object? data, int statusCode, string message = "Success") =>
        new(data, statusCode, message, Array.Empty<ApiError>());

    public static ApiResponse Failure(int statusCode, string message, IReadOnlyList<ApiError> errors) =>
        new(null, statusCode, message, errors);
}
