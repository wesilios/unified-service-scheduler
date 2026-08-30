using Scheduler.Infrastructure.ExternalServices;

namespace Scheduler.UnitTests.Infrastructure;

public class MockTechnicianServiceTests
{
    [Fact]
    public async Task ExistsAsync_EmptyGuid_ReturnsFalse()
    {
        var sut = new MockTechnicianService();
        Assert.False(await sut.ExistsAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExistsAsync_NonEmptyGuid_ReturnsTrue()
    {
        var sut = new MockTechnicianService();
        Assert.True(await sut.ExistsAsync(Guid.NewGuid()));
    }
}

public class MockServiceBayServiceTests
{
    [Fact]
    public async Task ExistsAsync_EmptyGuid_ReturnsFalse()
    {
        var sut = new MockServiceBayService();
        Assert.False(await sut.ExistsAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExistsAsync_NonEmptyGuid_ReturnsTrue()
    {
        var sut = new MockServiceBayService();
        Assert.True(await sut.ExistsAsync(Guid.NewGuid()));
    }
}
