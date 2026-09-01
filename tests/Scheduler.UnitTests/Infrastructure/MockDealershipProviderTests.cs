using Scheduler.Infrastructure.InternalServices;

namespace Scheduler.UnitTests.Infrastructure;

public class MockDealershipProviderTests
{
    private static readonly Guid KnownDealershipId = new("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetAsync_KnownId_ReturnsDealership()
    {
        var provider = new MockDealershipProvider();

        var dealership = await provider.GetAsync(KnownDealershipId);

        Assert.NotNull(dealership);
        Assert.Equal(KnownDealershipId, dealership!.Id);
        Assert.Equal("Downtown Dealership", dealership.Name);
        Assert.Equal(new TimeOnly(8, 0), dealership.OperatingHoursStart);
        Assert.Equal(new TimeOnly(17, 0), dealership.OperatingHoursEnd);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        var provider = new MockDealershipProvider();

        var dealership = await provider.GetAsync(Guid.NewGuid());

        Assert.Null(dealership);
    }
}
