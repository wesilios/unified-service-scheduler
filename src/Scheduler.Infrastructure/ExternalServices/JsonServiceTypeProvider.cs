using System.Text.Json;
using System.Text.Json.Serialization;
using Scheduler.Application.Interfaces;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.Infrastructure.ExternalServices;

// Loads the seed catalog once at startup into a Dictionary<string, ServiceType> keyed by
// code, for O(1) lookup — not a network call, not a database table. See Domain
// Assumptions > Service Type. A real service could replace this without changing callers.
public sealed class JsonServiceTypeProvider : IServiceTypeProvider
{
    private readonly IReadOnlyDictionary<string, ServiceType> _serviceTypes;

    public JsonServiceTypeProvider(string jsonFilePath)
    {
        var json = File.ReadAllText(jsonFilePath);
        var records = JsonSerializer.Deserialize<List<ServiceTypeRecord>>(json, JsonOptions) ?? [];

        _serviceTypes = records.ToDictionary(
            r => r.Code,
            r => new ServiceType(r.Code, r.Description, TimeSpan.FromMinutes(r.DurationMinutes)));
    }

    public Task<ServiceType?> TryGetAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_serviceTypes.GetValueOrDefault(code));

    public Task<IReadOnlyDictionary<string, ServiceType>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_serviceTypes);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private sealed class ServiceTypeRecord
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }
    }
}
