namespace Scheduler.Api.Contracts;

// Documentation-only mirror of ApiResponse with a typed Data property. Referenced solely from
// [ProducesResponseType] attributes on controller actions so the generated OpenAPI/Scalar
// schema shows the real envelope shape (Data/StatusCode/Message/Errors) with a concrete Data
// type per endpoint, instead of ApiResponse's untyped `object? Data`. Never constructed at
// runtime — ApiResponseWrapperFilter and ApiExceptionHandler still build the actual
// non-generic ApiResponse via ApiResponseFactory; the JSON shape the two produce is identical
// since the property names match.
public sealed record ApiResponseOf<TData>(TData? Data, int StatusCode, string Message, IReadOnlyList<ApiError> Errors);
