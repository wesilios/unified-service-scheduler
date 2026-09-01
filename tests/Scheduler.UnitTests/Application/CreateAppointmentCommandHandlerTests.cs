using Moq;
using Scheduler.Application.Commands;
using Scheduler.Application.Handlers;
using Scheduler.Application.Interfaces;
using Scheduler.Application.Results;
using Scheduler.Application.Services;
using Scheduler.Domain.Entities;
using Scheduler.Domain.Exceptions;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.UnitTests.Application;

public class CreateAppointmentCommandHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IDealershipRepository> _dealerships = new();
    private readonly Mock<ITechnicianService> _technicianService = new();
    private readonly Mock<IServiceBayService> _serviceBayService = new();
    private readonly Mock<IServiceTypeProvider> _serviceTypeProvider = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IAvailabilityCache> _availabilityCache = new();

    private static readonly Guid DealershipId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly Guid ServiceBayId = Guid.NewGuid();
    private static readonly DateTime StartTime = new(2026, 9, 7, 10, 0, 0); // Monday
    private static readonly ServiceType OilChange = new("OIL_CHANGE", "Oil Change", TimeSpan.FromMinutes(30));
    private static readonly Dealership Dealership = new(DealershipId, "Test", new TimeOnly(8, 0), new TimeOnly(17, 0));

    private static readonly CreateAppointmentCommand ValidCommand = new(
        "Juan Dela Cruz", "juan@example.com", "+639171234567",
        "Toyota - Vios - Vios G 2019", "OIL_CHANGE",
        DealershipId, TechnicianId, ServiceBayId, StartTime);

    private void SetupHappyPathDependencies()
    {
        _serviceTypeProvider.Setup(x => x.TryGet("OIL_CHANGE")).Returns(OilChange);
        _technicianService.Setup(x => x.ExistsAsync(TechnicianId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _serviceBayService.Setup(x => x.ExistsAsync(ServiceBayId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _dealerships.Setup(x => x.GetAsync(DealershipId, It.IsAny<CancellationToken>())).ReturnsAsync(Dealership);
        _appointments
            .Setup(x => x.GetOverlappingAsync(TechnicianId, ServiceBayId, It.IsAny<TimeRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());
    }

    private CreateAppointmentCommandHandler CreateSut()
    {
        var checker = new AppointmentAvailabilityChecker(
            _dealerships.Object, _technicianService.Object, _serviceBayService.Object,
            _serviceTypeProvider.Object, _appointments.Object);

        return new CreateAppointmentCommandHandler(
            checker, _appointments.Object, _notificationService.Object, _availabilityCache.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesAppointmentWithEmbeddedCustomer()
    {
        SetupHappyPathDependencies();

        var sut = CreateSut();

        var result = (AppointmentResult)await sut.HandleAsync(ValidCommand);

        Assert.Equal(AppointmentResultStatus.Success, result.Status);
        Assert.NotNull(result.Appointment);
        Assert.Equal("Juan Dela Cruz", result.Appointment!.Customer.Name);
        Assert.Equal("juan@example.com", result.Appointment.Customer.Email);
        Assert.Equal("+639171234567", result.Appointment.Customer.Phone);
        _appointments.Verify(x => x.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
        _availabilityCache.Verify(
            x => x.InvalidateAsync(TechnicianId, ServiceBayId, It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(
            x => x.SendConfirmationAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RepeatCustomer_EachBookingGetsItsOwnEmbeddedCustomer()
    {
        SetupHappyPathDependencies();
        var sut = CreateSut();

        var first = (AppointmentResult)await sut.HandleAsync(ValidCommand);
        var second = (AppointmentResult)await sut.HandleAsync(ValidCommand);

        Assert.Equal(AppointmentResultStatus.Success, first.Status);
        Assert.Equal(AppointmentResultStatus.Success, second.Status);
        Assert.NotEqual(first.Appointment!.Id, second.Appointment!.Id);
        Assert.Equal(first.Appointment.Customer.Email, second.Appointment.Customer.Email);
    }

    [Fact]
    public async Task HandleAsync_UnknownServiceType_ReturnsFailureWithoutTouchingAppointmentRepo()
    {
        _serviceTypeProvider.Setup(x => x.TryGet("OIL_CHANGE")).Returns((ServiceType?)null);
        var sut = CreateSut();

        var result = (AppointmentResult)await sut.HandleAsync(ValidCommand);

        Assert.Equal(AppointmentResultStatus.InvalidServiceType, result.Status);
        _appointments.Verify(x => x.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidTechnician_ReturnsInvalidResource()
    {
        SetupHappyPathDependencies();
        _technicianService.Setup(x => x.ExistsAsync(TechnicianId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();

        var result = (AppointmentResult)await sut.HandleAsync(ValidCommand);

        Assert.Equal(AppointmentResultStatus.InvalidResource, result.Status);
    }

    [Fact]
    public async Task HandleAsync_OutsideOperatingHours_ReturnsOutsideOperatingHours()
    {
        SetupHappyPathDependencies();
        var sut = CreateSut();
        var earlyCommand = ValidCommand with { StartTime = new DateTime(2026, 9, 7, 6, 0, 0) };

        var result = (AppointmentResult)await sut.HandleAsync(earlyCommand);

        Assert.Equal(AppointmentResultStatus.OutsideOperatingHours, result.Status);
    }

    [Fact]
    public async Task HandleAsync_OverlapOnReadCheck_ReturnsConflict()
    {
        SetupHappyPathDependencies();
        var existing = Appointment.Create(
            "Existing Customer", "existing@example.com", "+639170000000",
            DealershipId, "Toyota - Vios - Vios G 2019", "OIL_CHANGE",
            TechnicianId, ServiceBayId, new TimeRange(StartTime, StartTime.AddMinutes(30)));
        _appointments
            .Setup(x => x.GetOverlappingAsync(TechnicianId, ServiceBayId, It.IsAny<TimeRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        var sut = CreateSut();

        var result = (AppointmentResult)await sut.HandleAsync(ValidCommand);

        Assert.Equal(AppointmentResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task HandleAsync_InsertConflict_ReturnsConflict()
    {
        SetupHappyPathDependencies();
        _appointments
            .Setup(x => x.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AppointmentConflictException("lost the race"));

        var sut = CreateSut();

        var result = (AppointmentResult)await sut.HandleAsync(ValidCommand);

        Assert.Equal(AppointmentResultStatus.Conflict, result.Status);
        _notificationService.Verify(
            x => x.SendConfirmationAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
