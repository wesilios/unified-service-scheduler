using Moq;
using Scheduler.Application.Handlers;
using Scheduler.Application.Interfaces;
using Scheduler.Application.Queries;
using Scheduler.Application.Services;
using Scheduler.Domain.Entities;
using Scheduler.Domain.Repositories;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.UnitTests.Application;

public class CheckAvailabilityQueryHandlerTests
{
    private readonly Mock<IDealershipProvider> _dealershipProvider = new();
    private readonly Mock<ITechnicianProvider> _technicianProvider = new();
    private readonly Mock<IServiceBayProvider> _serviceBayProvider = new();
    private readonly Mock<IServiceTypeProvider> _serviceTypeProvider = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();

    private static readonly Guid DealershipId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly Guid ServiceBayId = Guid.NewGuid();
    private static readonly DateTime StartTime = new(2026, 9, 7, 10, 0, 0);
    private static readonly ServiceType OilChange = new("OIL_CHANGE", "Oil Change", TimeSpan.FromMinutes(30));
    private static readonly Dealership Dealership = new(DealershipId, "Test", new TimeOnly(8, 0), new TimeOnly(17, 0));

    private CheckAvailabilityQueryHandler CreateSut()
    {
        var checker = new AppointmentAvailabilityChecker(
            _dealershipProvider.Object, _technicianProvider.Object, _serviceBayProvider.Object,
            _serviceTypeProvider.Object, _appointments.Object);
        return new CheckAvailabilityQueryHandler(checker);
    }

    [Fact]
    public async Task HandleAsync_AvailableSlot_ReturnsAvailable()
    {
        _serviceTypeProvider.Setup(x => x.TryGet("OIL_CHANGE")).Returns(OilChange);
        _technicianProvider.Setup(x => x.ExistsAsync(TechnicianId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _serviceBayProvider.Setup(x => x.ExistsAsync(ServiceBayId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _dealershipProvider.Setup(x => x.GetAsync(DealershipId, It.IsAny<CancellationToken>())).ReturnsAsync(Dealership);
        _appointments
            .Setup(x => x.GetOverlappingAsync(TechnicianId, ServiceBayId, It.IsAny<TimeRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());

        var sut = CreateSut();
        var query = new CheckAvailabilityQuery(DealershipId, TechnicianId, ServiceBayId, "OIL_CHANGE", StartTime);

        var result = await sut.HandleAsync(query);

        Assert.Equal(AvailabilityStatus.Available, result.Status);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task HandleAsync_UnknownServiceType_ReturnsInvalidServiceType()
    {
        _serviceTypeProvider.Setup(x => x.TryGet("UNKNOWN")).Returns((ServiceType?)null);

        var sut = CreateSut();
        var query = new CheckAvailabilityQuery(DealershipId, TechnicianId, ServiceBayId, "UNKNOWN", StartTime);

        var result = await sut.HandleAsync(query);

        Assert.Equal(AvailabilityStatus.InvalidServiceType, result.Status);
        Assert.NotNull(result.Reason);
    }
}
