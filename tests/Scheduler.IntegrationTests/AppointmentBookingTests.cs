using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Scheduler.IntegrationTests;

public class AppointmentBookingTests : IDisposable
{
    // Seeded by the InitialCreate migration — see SchedulerDbContext.OnModelCreating.
    private static readonly Guid DealershipId = new("11111111-1111-1111-1111-111111111111");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SchedulerApiFactory _factory = new();
    private readonly HttpClient _client;

    public AppointmentBookingTests()
    {
        _client = _factory.CreateClient();
    }

    private static object BookingRequest(
        Guid technicianId,
        Guid serviceBayId,
        DateTime startTime,
        string email = "juan@example.com",
        string phone = "+639171234567",
        string serviceTypeCode = "OIL_CHANGE",
        string vehicle = "Toyota - Vios - Vios G 2019") => new
    {
        customerName = "Juan Dela Cruz",
        customerEmail = email,
        customerPhone = phone,
        vehicle,
        serviceTypeCode,
        dealershipId = DealershipId,
        technicianId,
        serviceBayId,
        startTime
    };

    [Fact]
    public async Task CreateAppointment_ValidRequest_Returns201WithSlots()
    {
        var response = await _client.PostAsJsonAsync(
            "/appointments", BookingRequest(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 9, 7, 10, 0, 0)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AppointmentResponse>>(JsonOptions);
        Assert.NotNull(envelope?.Data);
        Assert.Equal(201, envelope!.StatusCode);
        Assert.Equal("Success", envelope.Message);
        Assert.Empty(envelope.Errors);
        // 30-min OIL_CHANGE -> ceil(30/15)=2 slots * 2 resources = 4
        Assert.Equal(4, envelope.Data!.Slots.Count);
    }

    [Fact]
    public async Task CreateAppointment_SameSlotTwice_SecondReturns409()
    {
        var technicianId = Guid.NewGuid();
        var serviceBayId = Guid.NewGuid();
        var startTime = new DateTime(2026, 9, 7, 11, 0, 0);

        var first = await _client.PostAsJsonAsync("/appointments", BookingRequest(technicianId, serviceBayId, startTime));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/appointments", BookingRequest(technicianId, serviceBayId, startTime));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_OutsideOperatingHours_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/appointments", BookingRequest(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 9, 7, 7, 0, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // A single business-rule failure: Data is null, one entry in Errors carrying the
        // machine-readable AppointmentResultStatus name as ErrorCode.
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(JsonOptions);
        Assert.Null(envelope!.Data);
        Assert.Equal(400, envelope.StatusCode);
        var error = Assert.Single(envelope.Errors);
        Assert.Equal("OutsideOperatingHours", error.ErrorCode);
        Assert.Equal("Requested time is outside dealership operating hours.", error.ErrorMessage);
    }

    [Fact]
    public async Task CreateAppointment_Sunday_Returns400()
    {
        // 2026-09-06 is a Sunday.
        var response = await _client.PostAsJsonAsync(
            "/appointments", BookingRequest(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 9, 6, 10, 0, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_InvalidTechnician_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/appointments", BookingRequest(Guid.Empty, Guid.NewGuid(), new DateTime(2026, 9, 7, 10, 0, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_EmptyVehicleField_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/appointments",
            BookingRequest(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 9, 7, 10, 0, 0), vehicle: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_MultipleValidationFailures_ReturnsOneErrorPerField()
    {
        // Empty vehicle AND an invalid email in the same request — FluentValidation doesn't
        // cascade-stop across independent RuleFor chains, so both fail together. This is the
        // "complex situation where the request encounters multiple errors at the same time"
        // the ApiResponse envelope's Errors array exists for.
        var request = new
        {
            customerName = "Test Customer",
            customerEmail = "not-an-email",
            customerPhone = "+639170000000",
            vehicle = "",
            serviceTypeCode = "OIL_CHANGE",
            dealershipId = DealershipId,
            technicianId = Guid.NewGuid(),
            serviceBayId = Guid.NewGuid(),
            startTime = new DateTime(2026, 9, 7, 10, 0, 0)
        };

        var response = await _client.PostAsJsonAsync("/appointments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(JsonOptions);
        Assert.Null(envelope!.Data);
        Assert.True(envelope.Errors.Count >= 2, "Expected at least one error per invalid field.");
        Assert.Contains(envelope.Errors, e => e.ErrorCode == "Vehicle");
        Assert.Contains(envelope.Errors, e => e.ErrorCode == "CustomerEmail");
    }

    [Fact]
    public async Task CreateAppointment_SameCustomerTwice_ReusesCustomerId()
    {
        var first = await _client.PostAsJsonAsync(
            "/appointments",
            BookingRequest(
                Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 9, 7, 9, 0, 0),
                email: "repeat@example.com", phone: "+639170001111"));
        var firstEnvelope = await first.Content.ReadFromJsonAsync<ApiEnvelope<AppointmentResponse>>(JsonOptions);

        var second = await _client.PostAsJsonAsync(
            "/appointments",
            BookingRequest(
                Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 9, 7, 13, 0, 0),
                email: "repeat@example.com", phone: "+639170001111"));
        var secondEnvelope = await second.Content.ReadFromJsonAsync<ApiEnvelope<AppointmentResponse>>(JsonOptions);

        Assert.NotEqual(Guid.Empty, firstEnvelope!.Data!.CustomerId);
        Assert.Equal(firstEnvelope.Data!.CustomerId, secondEnvelope!.Data!.CustomerId);
    }

    [Fact]
    public async Task CreateAppointment_ConcurrentRequestsForSameSlot_ExactlyOneSucceeds()
    {
        var technicianId = Guid.NewGuid();
        var serviceBayId = Guid.NewGuid();
        var startTime = new DateTime(2026, 9, 7, 14, 0, 0);

        const int concurrentRequests = 8;
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(i => _client.PostAsJsonAsync(
                "/appointments",
                BookingRequest(
                    technicianId, serviceBayId, startTime,
                    email: $"race{i}@example.com", phone: $"+63917000{i:D4}")))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // This is the test that actually validates the concurrency-safety requirement
        // (Agent.md core requirement #3) — the AppointmentSlot unique constraint, not the
        // application-level read-check, is what makes this true under genuine parallelism.
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(concurrentRequests - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task CheckAvailability_BookedSlot_ReturnsUnavailable()
    {
        var technicianId = Guid.NewGuid();
        var serviceBayId = Guid.NewGuid();
        var startTime = new DateTime(2026, 9, 7, 15, 0, 0);

        await _client.PostAsJsonAsync("/appointments", BookingRequest(technicianId, serviceBayId, startTime));

        var response = await _client.GetAsync(
            $"/appointments/availability?dealershipId={DealershipId}&technicianId={technicianId}" +
            $"&serviceBayId={serviceBayId}&serviceTypeCode=OIL_CHANGE&startTime={startTime:o}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("data").GetProperty("available").GetBoolean());
    }

    [Fact]
    public async Task CheckAvailability_FreeSlot_ReturnsAvailable()
    {
        var response = await _client.GetAsync(
            $"/appointments/availability?dealershipId={DealershipId}&technicianId={Guid.NewGuid()}" +
            $"&serviceBayId={Guid.NewGuid()}&serviceTypeCode=OIL_CHANGE&startTime={new DateTime(2026, 9, 7, 16, 0, 0):o}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("data").GetProperty("available").GetBoolean());
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Request_WithoutCorrelationIdHeader_AutoGeneratesOne()
    {
        // No API gateway/load balancer sits in front in this assessment (see
        // architecture.md's Assessment Scope Notes), so this is the path every real
        // request here actually takes: CorrelationIdMiddlewareExtensions mints a fresh
        // Guid.NewGuid() when the caller doesn't supply X-Correlation-Id.
        var response = await _client.GetAsync("/health");

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        var correlationId = Assert.Single(values!);
        Assert.True(Guid.TryParse(correlationId, out var parsed));
        Assert.NotEqual(Guid.Empty, parsed);
    }

    [Fact]
    public async Task Request_WithCorrelationIdHeader_EchoesItBack()
    {
        // With a gateway/load balancer in front, this is the path that lets a single
        // correlation id be traced from that edge component down through this service.
        var correlationId = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Equal(correlationId, Assert.Single(values!));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}

// Test-owned DTOs matching the API's wire format. ApiEnvelope<T> mirrors
// Scheduler.Api.Contracts.ApiResponse (every response is wrapped in this — see
// ApiResponseWrapperFilter) but typed, so tests can deserialize Data directly instead of
// re-parsing JsonElement each time. AppointmentResponse is deliberately not the domain
// Appointment entity, which has private setters and can't be deserialized by System.Text.Json.
internal sealed record ApiEnvelope<T>(T? Data, int StatusCode, string Message, List<ApiErrorResponse> Errors);

internal sealed record ApiErrorResponse(string ErrorCode, string ErrorMessage);

internal sealed record AppointmentResponse(Guid Id, Guid CustomerId, List<JsonElement> Slots);
