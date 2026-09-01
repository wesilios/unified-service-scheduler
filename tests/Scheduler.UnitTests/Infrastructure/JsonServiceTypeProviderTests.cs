using Scheduler.Infrastructure.ExternalServices;

namespace Scheduler.UnitTests.Infrastructure;

public class JsonServiceTypeProviderTests : IDisposable
{
    private readonly string _tempFile;

    public JsonServiceTypeProviderTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"servicetypes-{Guid.NewGuid()}.json");
        File.WriteAllText(_tempFile, """
            [
              { "code": "OIL_CHANGE", "description": "Oil Change", "durationMinutes": 30 },
              { "code": "TIRE_CHANGE", "description": "Tire Change / Replacement", "durationMinutes": 60 }
            ]
            """);
    }

    [Fact]
    public async Task TryGetAsync_KnownCode_ReturnsServiceType()
    {
        var provider = new JsonServiceTypeProvider(_tempFile);

        var result = await provider.TryGetAsync("OIL_CHANGE");

        Assert.NotNull(result);
        Assert.Equal("Oil Change", result!.Description);
        Assert.Equal(TimeSpan.FromMinutes(30), result.Duration);
    }

    [Fact]
    public async Task TryGetAsync_UnknownCode_ReturnsNull()
    {
        var provider = new JsonServiceTypeProvider(_tempFile);

        var result = await provider.TryGetAsync("UNKNOWN");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntries()
    {
        var provider = new JsonServiceTypeProvider(_tempFile);

        var all = await provider.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains("OIL_CHANGE", all.Keys);
        Assert.Contains("TIRE_CHANGE", all.Keys);
    }

    public void Dispose() => File.Delete(_tempFile);
}
