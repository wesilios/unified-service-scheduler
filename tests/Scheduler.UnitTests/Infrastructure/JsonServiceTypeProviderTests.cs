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
    public void TryGet_KnownCode_ReturnsServiceType()
    {
        var sut = new JsonServiceTypeProvider(_tempFile);

        var result = sut.TryGet("OIL_CHANGE");

        Assert.NotNull(result);
        Assert.Equal("Oil Change", result!.Description);
        Assert.Equal(TimeSpan.FromMinutes(30), result.Duration);
    }

    [Fact]
    public void TryGet_UnknownCode_ReturnsNull()
    {
        var sut = new JsonServiceTypeProvider(_tempFile);

        var result = sut.TryGet("UNKNOWN");

        Assert.Null(result);
    }

    [Fact]
    public void GetAll_ReturnsAllEntries()
    {
        var sut = new JsonServiceTypeProvider(_tempFile);

        var all = sut.GetAll();

        Assert.Equal(2, all.Count);
        Assert.Contains("OIL_CHANGE", all.Keys);
        Assert.Contains("TIRE_CHANGE", all.Keys);
    }

    public void Dispose() => File.Delete(_tempFile);
}
