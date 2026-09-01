using Moq;
using Scheduler.Application.Interfaces;
using Scheduler.Application.Queries;
using Scheduler.Application.Services;
using Scheduler.Domain.Entities;
using Scheduler.Domain.Repositories;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.UnitTests.Application;

public class AppointmentAvailabilityCheckerTests
{
    private readonly Mock<IDealershipProvider> _dealershipProvider = new();
    private readonly Mock<ITechnicianProvider> _technicianProvider = new();
    private readonly Mock<IServiceBayProvider> _serviceBayProvider = new();
    private readonly Mock<IServiceTypeProvider> _serviceTypeProvider = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();

    private static readonly Guid DealershipId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly Guid ServiceBayId = Guid.NewGuid();
    private static readonly DateTime StartTime = new(2026, 9, 7, 10, 0, 0); // Monday
    private static readonly ServiceType OilChange = new("OIL_CHANGE", "Oil Change", TimeSpan.FromMinutes(30));
    private static readonly Dealership Dealership = new(DealershipId, "Test", new TimeOnly(8, 0), new TimeOnly(17, 0));

    private AppointmentAvailabilityChecker CreateSut() => new(
        _dealershipProvider.Object, _technicianProvider.Object, _serviceBayProvider.Object,
        _serviceTypeProvider.Object, _appointments.Object);

    private void SetupHappyPathDependencies()
    {
        _serviceTypeProvider.Setup(x => x.TryGetAsync("OIL_CHANGE", It.IsAny<CancellationToken>())).ReturnsAsync(OilChange);
        _technicianProvider.Setup(x => x.ExistsAsync(TechnicianId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _serviceBayProvider.Setup(x => x.ExistsAsync(ServiceBayId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _dealershipProvider.Setup(x => x.GetAsync(DealershipId, It.IsAny<CancellationToken>())).ReturnsAsync(Dealership);
        _appointments
            .Setup(x => x.GetOverlappingAsync(TechnicianId, ServiceBayId, It.IsAny<TimeRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());
    }

    [Fact]
    public async Task CheckAsync_UnknownServiceType_ReturnsInvalidServiceType()
    {
        _serviceTypeProvider.Setup(x => x.TryGetAsync("UNKNOWN", It.IsAny<CancellationToken>())).ReturnsAsync((ServiceType?)null);
        var sut = CreateSut();

        var result = await sut.CheckAsync(DealershipId, TechnicianId, ServiceBayId, "UNKNOWN", StartTime);

        Assert.Equal(AvailabilityStatus.InvalidServiceType, result.Status);
    }

    [Fact]
    public async Task CheckAsync_TechnicianDoesNotExist_ReturnsInvalidResource()
    {
        SetupHappyPathDependencies();
        _technicianProvider.Setup(x => x.ExistsAsync(TechnicianId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();

        var result = await sut.CheckAsync(DealershipId, TechnicianId, ServiceBayId, "OIL_CHANGE", StartTime);

        Assert.Equal(AvailabilityStatus.InvalidResource, result.Status);
    }

    [Fact]
    public async Task CheckAsync_ServiceBayDoesNotExist_ReturnsInvalidResource()
    {
        SetupHappyPathDependencies();
        _serviceBayProvider.Setup(x => x.ExistsAsync(ServiceBayId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();

        var result = await sut.CheckAsync(DealershipId, TechnicianId, ServiceBayId, "OIL_CHANGE", StartTime);

        Assert.Equal(AvailabilityStatus.InvalidResource, result.Status);
    }

    [Fact]
    public async Task CheckAsync_DealershipNotFound_ReturnsInvalidResource()
    {
        SetupHappyPathDependencies();
        _dealershipProvider.Setup(x => x.GetAsync(DealershipId, It.IsAny<CancellationToken>())).ReturnsAsync((Dealership?)null);
        var sut = CreateSut();

        var result = await sut.CheckAsync(DealershipId, TechnicianId, ServiceBayId, "OIL_CHANGE", StartTime);

        Assert.Equal(AvailabilityStatus.InvalidResource, result.Status);
    }

    [Fact]
    public async Task CheckAsync_OutsideOperatingHours_ReturnsOutsideOperatingHours()
    {
        SetupHappyPathDependencies();
        var sut = CreateSut();

        var earlyMorning = new DateTime(2026, 9, 7, 6, 0, 0);
        var result = await sut.CheckAsync(DealershipId, TechnicianId, ServiceBayId, "OIL_CHANGE", earlyMorning);

        Assert.Equal(AvailabilityStatus.OutsideOperatingHours, result.Status);
    }

    [Fact]
    public async Task CheckAsync_OverlappingAppointmentExists_ReturnsUnavailable()
    {
        SetupHappyPathDependencies();
        var existing = Appointment.Create(
            "Juan Dela Cruz", "juan@example.com", "+639171234567",
            DealershipId, "Toyota - Vios - Vios G 2019", "OIL_CHANGE",
            TechnicianId, ServiceBayId, new TimeRange(StartTime, StartTime.AddMinutes(30)));
        _appointments
            .Setup(x => x.GetOverlappingAsync(TechnicianId, ServiceBayId, It.IsAny<TimeRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);
        var sut = CreateSut();

        var result = await sut.CheckAsync(DealershipId, TechnicianId, ServiceBayId, "OIL_CHANGE", StartTime);

        Assert.Equal(AvailabilityStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task CheckAsync_AllValid_ReturnsAvailableWithServiceTypeAndRange()
    {
        SetupHappyPathDependencies();
        var sut = CreateSut();

        var result = await sut.CheckAsync(DealershipId, TechnicianId, ServiceBayId, "OIL_CHANGE", StartTime);

        Assert.Equal(AvailabilityStatus.Available, result.Status);
        Assert.Equal(OilChange, result.ServiceType);
        Assert.NotNull(result.Range);
        Assert.Equal(StartTime, result.Range!.Start);
        Assert.Equal(StartTime.AddMinutes(30), result.Range.End);
    }
}
