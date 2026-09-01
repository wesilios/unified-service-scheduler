namespace Scheduler.Api.Contracts;

// The single response envelope every endpoint returns — success or failure. Only
// ApiResponseWrapperFilter and ApiExceptionHandler construct these (via ApiResponseFactory);
// controllers never build one directly, so the shape lives in exactly one place. See
// ApiResponseFactory for why.
public sealed record ApiResponse(object? Data, int StatusCode, string Message, IReadOnlyList<ApiError> Errors);

// ErrorCode is the FluentValidation property name for a validation failure (e.g.
// "CustomerName"), or the business-result status name for a domain failure (e.g.
// "OutsideOperatingHours", "Conflict") — always machine-readable, never the human sentence.
public sealed record ApiError(string ErrorCode, string ErrorMessage);
