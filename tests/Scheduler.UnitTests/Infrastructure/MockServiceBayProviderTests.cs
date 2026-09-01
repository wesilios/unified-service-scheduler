using Scheduler.Infrastructure.InternalServices;

namespace Scheduler.UnitTests.Infrastructure;

public class MockServiceBayProviderTests
{
    [Fact]
    public async Task ExistsAsync_EmptyGuid_ReturnsFalse()
    {
        var provider = new MockServiceBayProvider();
        Assert.False(await provider.ExistsAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExistsAsync_NonEmptyGuid_ReturnsTrue()
    {
        var provider = new MockServiceBayProvider();
        Assert.True(await provider.ExistsAsync(Guid.NewGuid()));
    }
}
