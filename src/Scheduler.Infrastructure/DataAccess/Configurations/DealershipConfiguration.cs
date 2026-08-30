using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduler.Domain.Entities;

namespace Scheduler.Infrastructure.DataAccess.Configurations;

public sealed class DealershipConfiguration : IEntityTypeConfiguration<Dealership>
{
    public void Configure(EntityTypeBuilder<Dealership> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);

        // Seed a single default dealership (per Domain Assumptions: Mon-Sat 08:00-17:00)
        // so the booking flow has a real DealershipId to reference without a staff/admin
        // API to create one — that surface is documented but not implemented.
        builder.HasData(new
        {
            Id = new Guid("11111111-1111-1111-1111-111111111111"),
            Name = "Downtown Dealership",
            OperatingHoursStart = new TimeOnly(8, 0),
            OperatingHoursEnd = new TimeOnly(17, 0)
        });
    }
}
