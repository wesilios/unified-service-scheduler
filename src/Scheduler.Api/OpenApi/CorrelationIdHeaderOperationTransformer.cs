using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Scheduler.Api.OpenApi;

// Documents the X-Correlation-Id header that CorrelationIdMiddlewareExtensions.UseCorrelationId
// reads on every request (or mints one if absent) and always echoes back on the response. The
// OpenAPI generator can't see this on its own — it's read directly off
// HttpContext.Request.Headers in middleware, never bound as an action parameter — so it has to
// be added to every operation explicitly here. Registered globally in Program.cs
// (AddOpenApi(options => options.AddOperationTransformer<...>())) rather than per-controller
// because the header applies to every endpoint, not just Appointments.
public sealed class CorrelationIdHeaderOperationTransformer : IOpenApiOperationTransformer
{
    private const string HeaderName = "X-Correlation-Id";

    // A stable, obviously-fake UUID rather than a randomly generated one, so this file's
    // generated example doesn't churn on every doc regeneration.
    private static readonly JsonNode ExampleValue = JsonValue.Create("2fa66c3e-8b1a-4c1d-9c3a-2b7f6e6b6a10")!;

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderName,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Correlates this request's logs and trace across services. Echoed back on the response header of the same name; a new one is generated when this is omitted.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            Example = ExampleValue.DeepClone()
        });

        foreach (var response in operation.Responses.Values)
        {
            // IOpenApiResponse.Headers is get-only and null by default on a freshly generated
            // OpenApiResponse — only the concrete type exposes a settable property.
            if (response is not OpenApiResponse concreteResponse)
            {
                continue;
            }

            concreteResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
            concreteResponse.Headers[HeaderName] = new OpenApiHeader
            {
                Description = "The correlation ID for this request — echoed from the request header, or generated if it was absent.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                Example = ExampleValue.DeepClone()
            };
        }

        return Task.CompletedTask;
    }
}
