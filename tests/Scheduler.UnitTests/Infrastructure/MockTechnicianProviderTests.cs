using Scheduler.Infrastructure.InternalServices;

namespace Scheduler.UnitTests.Infrastructure;

public class MockTechnicianProviderTests
{
    [Fact]
    public async Task ExistsAsync_EmptyGuid_ReturnsFalse()
    {
        var provider = new MockTechnicianProvider();
        Assert.False(await provider.ExistsAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExistsAsync_NonEmptyGuid_ReturnsTrue()
    {
        var provider = new MockTechnicianProvider();
        Assert.True(await provider.ExistsAsync(Guid.NewGuid()));
    }
}
