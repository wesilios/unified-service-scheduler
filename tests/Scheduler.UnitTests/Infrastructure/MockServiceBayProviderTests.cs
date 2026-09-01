using Scheduler.Infrastructure.ExternalServices;

namespace Scheduler.UnitTests.Infrastructure;

public class MockServiceBayProviderTests
{
    [Fact]
    public async Task ExistsAsync_EmptyGuid_ReturnsFalse()
    {
        var sut = new MockServiceBayProvider();
        Assert.False(await sut.ExistsAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExistsAsync_NonEmptyGuid_ReturnsTrue()
    {
        var sut = new MockServiceBayProvider();
        Assert.True(await sut.ExistsAsync(Guid.NewGuid()));
    }
}
