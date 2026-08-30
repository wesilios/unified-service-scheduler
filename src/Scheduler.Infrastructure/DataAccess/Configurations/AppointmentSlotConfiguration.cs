using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduler.Domain.Entities;

namespace Scheduler.Infrastructure.DataAccess.Configurations;

public sealed class AppointmentSlotConfiguration : IEntityTypeConfiguration<AppointmentSlot>
{
    public void Configure(EntityTypeBuilder<AppointmentSlot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ResourceKind).HasConversion<string>().HasMaxLength(20);

        // The concurrency guarantee: a concurrent conflicting booking fails this
        // constraint on insert and the whole transaction rolls back. See Data Model.
        builder.HasIndex(s => new { s.ResourceKind, s.ResourceId, s.SlotStart }).IsUnique();
    }
}
