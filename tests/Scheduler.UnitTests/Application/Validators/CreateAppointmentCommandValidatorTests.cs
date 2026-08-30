using Scheduler.Application.Commands;
using Scheduler.Application.Validators;

namespace Scheduler.UnitTests.Application.Validators;

public class CreateAppointmentCommandValidatorTests
{
    private readonly CreateAppointmentCommandValidator _validator = new();

    private static CreateAppointmentCommand ValidCommand() => new(
        "Juan Dela Cruz", "juan@example.com", "+639171234567",
        "Toyota - Vios - Vios G 2019", "OIL_CHANGE",
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        DateTime.UtcNow.AddDays(1));

    [Fact]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyCustomerName_Fails()
    {
        var command = ValidCommand() with { CustomerName = "" };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.CustomerName));
    }

    [Fact]
    public async Task Validate_InvalidEmail_Fails()
    {
        var command = ValidCommand() with { CustomerEmail = "not-an-email" };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.CustomerEmail));
    }

    [Fact]
    public async Task Validate_EmptyPhone_Fails()
    {
        var command = ValidCommand() with { CustomerPhone = "" };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyVehicle_Fails()
    {
        var command = ValidCommand() with { Vehicle = "" };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyServiceTypeCode_Fails()
    {
        var command = ValidCommand() with { ServiceTypeCode = "" };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyDealershipId_Fails()
    {
        var command = ValidCommand() with { DealershipId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyTechnicianId_Fails()
    {
        var command = ValidCommand() with { TechnicianId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyServiceBayId_Fails()
    {
        var command = ValidCommand() with { ServiceBayId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_PastStartTime_Fails()
    {
        var command = ValidCommand() with { StartTime = DateTime.UtcNow.AddDays(-1) };
        var result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }
}
