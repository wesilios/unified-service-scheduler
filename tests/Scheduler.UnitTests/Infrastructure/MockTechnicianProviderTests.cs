using Scheduler.Infrastructure.InternalServices;

namespace Scheduler.UnitTests.Infrastructure;

public class MockTechnicianProviderTests
{
    [Fact]
    public async Task ExistsAsync_EmptyGuid_ReturnsFalse()
    {
        var sut = new MockTechnicianProvider();
        Assert.False(await sut.ExistsAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExistsAsync_NonEmptyGuid_ReturnsTrue()
    {
        var sut = new MockTechnicianProvider();
        Assert.True(await sut.ExistsAsync(Guid.NewGuid()));
    }
}
