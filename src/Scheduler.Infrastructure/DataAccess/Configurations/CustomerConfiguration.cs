using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduler.Domain.Entities;

namespace Scheduler.Infrastructure.DataAccess.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength(50);

        // Guest checkout: no login, so Email+Phone is the de-facto identity key —
        // repeat customers are matched to their existing record instead of duplicated.
        // This constraint is what makes that matching race-safe under concurrency
        // (same pattern as AppointmentSlot). See CreateAppointmentCommandHandler.
        builder.HasIndex(c => new { c.Email, c.Phone }).IsUnique();
    }
}
