using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Scheduler.Api.OpenApi;

// Attaches hand-written sample request/response bodies to the Appointments operations.
// [ProducesResponseType] alone gives the OpenAPI generator a schema, not a realistic payload —
// Scalar/OpenAPI tooling only shows a curated "Example" in the docs when one is set explicitly
// via OpenApiMediaType.Example, which this does per status code so /scalar/v1 has something
// concrete to render instead of an empty or schema-inferred stub.
public sealed class AppointmentsExampleOperationTransformer : IOpenApiOperationTransformer
{
    private const string ContentType = "application/json";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var relativePath = context.Description.RelativePath;
        var method = context.Description.HttpMethod;

        if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) && relativePath == "appointments")
        {
            SetRequestExample(operation, CreateAppointmentRequestExample);
            SetResponseExample(operation, "201", CreateAppointmentSuccessExample);
            SetResponseExamples(operation, "400", new()
            {
                ["ValidationError"] = ("One or more request fields failed validation.", ValidationErrorExample),
                ["OutsideOperatingHours"] = ("The requested time falls outside the dealership's operating hours.", OutsideOperatingHoursExample)
            });
            SetResponseExample(operation, "409", ConflictExample);
        }
        else if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && relativePath == "appointments/availability")
        {
            SetResponseExample(operation, "200", AvailabilitySuccessExample);
            SetResponseExample(operation, "400", ValidationErrorExample);
        }

        return Task.CompletedTask;
    }

    private static void SetRequestExample(OpenApiOperation operation, Func<JsonNode> factory)
    {
        if (operation.RequestBody?.Content.TryGetValue(ContentType, out var mediaType) is true)
        {
            mediaType.Example = factory();
        }
    }

    private static void SetResponseExample(OpenApiOperation operation, string statusCode, Func<JsonNode> factory)
    {
        if (operation.Responses.TryGetValue(statusCode, out var response) &&
            response.Content.TryGetValue(ContentType, out var mediaType))
        {
            mediaType.Example = factory();
        }
    }

    private static void SetResponseExamples(
        OpenApiOperation operation,
        string statusCode,
        Dictionary<string, (string Summary, Func<JsonNode> Factory)> examples)
    {
        if (!operation.Responses.TryGetValue(statusCode, out var response) ||
            !response.Content.TryGetValue(ContentType, out var mediaType))
        {
            return;
        }

        mediaType.Examples = examples.ToDictionary(
            kvp => kvp.Key,
            IOpenApiExample (kvp) => new OpenApiExample { Summary = kvp.Value.Summary, Value = kvp.Value.Factory() });
    }

    private static JsonNode CreateAppointmentRequestExample() => JsonNode.Parse("""
        {
          "customerName": "Jane Doe",
          "customerEmail": "jane.doe@example.com",
          "customerPhone": "555-0100",
          "vehicle": "2019 Honda Civic",
          "serviceTypeCode": "OIL_CHANGE",
          "dealershipId": "8f14e45f-ceea-467e-bd23-8d2a3c8a1e11",
          "technicianId": "b3f8c9d2-1a2b-4c3d-9e4f-5a6b7c8d9e0f",
          "serviceBayId": "c4a9d8e3-2b3c-5d4e-8f5a-6b7c8d9e0f1a",
          "startTime": "2026-09-10T09:00:00Z"
        }
        """)!;

    private static JsonNode CreateAppointmentSuccessExample() => JsonNode.Parse("""
        {
          "data": {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "customer": {
              "name": "Jane Doe",
              "email": "jane.doe@example.com",
              "phone": "555-0100"
            },
            "dealershipId": "8f14e45f-ceea-467e-bd23-8d2a3c8a1e11",
            "vehicle": "2019 Honda Civic",
            "serviceTypeCode": "OIL_CHANGE",
            "technicianId": "b3f8c9d2-1a2b-4c3d-9e4f-5a6b7c8d9e0f",
            "serviceBayId": "c4a9d8e3-2b3c-5d4e-8f5a-6b7c8d9e0f1a",
            "duration": {
              "start": "2026-09-10T09:00:00Z",
              "end": "2026-09-10T09:30:00Z"
            },
            "status": 0,
            "createdAt": "2026-09-02T14:32:10Z",
            "slots": [
              { "id": "9c1e2b3a-0d1e-4f2a-8b3c-1d2e3f4a5b6c", "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "resourceKind": 0, "resourceId": "b3f8c9d2-1a2b-4c3d-9e4f-5a6b7c8d9e0f", "slotStart": "2026-09-10T09:00:00Z" },
              { "id": "1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c8d", "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "resourceKind": 1, "resourceId": "c4a9d8e3-2b3c-5d4e-8f5a-6b7c8d9e0f1a", "slotStart": "2026-09-10T09:00:00Z" },
              { "id": "2b3c4d5e-6f7a-4b2c-8d3e-4f5a6b7c8d9e", "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "resourceKind": 0, "resourceId": "b3f8c9d2-1a2b-4c3d-9e4f-5a6b7c8d9e0f", "slotStart": "2026-09-10T09:15:00Z" },
              { "id": "3c4d5e6f-7a8b-4c3d-9e4f-5a6b7c8d9e0f", "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "resourceKind": 1, "resourceId": "c4a9d8e3-2b3c-5d4e-8f5a-6b7c8d9e0f1a", "slotStart": "2026-09-10T09:15:00Z" }
            ]
          },
          "statusCode": 201,
          "message": "Success",
          "errors": []
        }
        """)!;

    private static JsonNode ValidationErrorExample() => JsonNode.Parse("""
        {
          "data": null,
          "statusCode": 400,
          "message": "One or more validation errors occurred.",
          "errors": [
            { "errorCode": "CustomerName", "errorMessage": "'Customer Name' must not be empty." },
            { "errorCode": "StartTime", "errorMessage": "'Start Time' must be in the future." }
          ]
        }
        """)!;

    private static JsonNode OutsideOperatingHoursExample() => JsonNode.Parse("""
        {
          "data": null,
          "statusCode": 400,
          "message": "The requested time is outside the dealership's operating hours.",
          "errors": [
            { "errorCode": "OutsideOperatingHours", "errorMessage": "The requested time is outside the dealership's operating hours." }
          ]
        }
        """)!;

    private static JsonNode ConflictExample() => JsonNode.Parse("""
        {
          "data": null,
          "statusCode": 409,
          "message": "The requested Technician or Service Bay slot is no longer available.",
          "errors": [
            { "errorCode": "Conflict", "errorMessage": "The requested Technician or Service Bay slot is no longer available." }
          ]
        }
        """)!;

    private static JsonNode AvailabilitySuccessExample() => JsonNode.Parse("""
        {
          "data": {
            "available": true,
            "status": "Available",
            "reason": null
          },
          "statusCode": 200,
          "message": "Success",
          "errors": []
        }
        """)!;
}
