using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduler.Domain.Entities;

namespace Scheduler.Infrastructure.DataAccess.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Vehicle).IsRequired().HasMaxLength(200);
        builder.Property(a => a.ServiceTypeCode).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(a => a.Duration, duration =>
        {
            duration.Property(r => r.Start).HasColumnName("StartTime").IsRequired();
            duration.Property(r => r.End).HasColumnName("EndTime").IsRequired();
        });
        builder.Navigation(a => a.Duration).IsRequired();

        builder.HasMany(a => a.Slots)
            .WithOne()
            .HasForeignKey(s => s.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Slots).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
