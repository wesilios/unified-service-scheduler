using Scheduler.Application.Queries;
using Scheduler.Application.Validators;

namespace Scheduler.UnitTests.Application.Validators;

public class CheckAvailabilityQueryValidatorTests
{
    private readonly CheckAvailabilityQueryValidator _validator = new();

    private static CheckAvailabilityQuery ValidQuery() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "OIL_CHANGE", DateTime.UtcNow.AddDays(1));

    [Fact]
    public async Task Validate_ValidQuery_Passes()
    {
        var result = await _validator.ValidateAsync(ValidQuery());
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyDealershipId_Fails()
    {
        var query = ValidQuery() with { DealershipId = Guid.Empty };
        var result = await _validator.ValidateAsync(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyTechnicianId_Fails()
    {
        var query = ValidQuery() with { TechnicianId = Guid.Empty };
        var result = await _validator.ValidateAsync(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyServiceTypeCode_Fails()
    {
        var query = ValidQuery() with { ServiceTypeCode = "" };
        var result = await _validator.ValidateAsync(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_PastStartTime_Fails()
    {
        var query = ValidQuery() with { StartTime = DateTime.UtcNow.AddDays(-1) };
        var result = await _validator.ValidateAsync(query);
        Assert.False(result.IsValid);
    }
}
