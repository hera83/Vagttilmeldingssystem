using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Shifts_Capacity", "\"Capacity\" BETWEEN 1 AND 500");
            t.HasCheckConstraint("CK_Shifts_Period", "\"EndUtc\" > \"StartUtc\"");
        });

        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);

        builder.HasIndex(x => new { x.EventId, x.StartUtc });
        builder.HasIndex(x => new { x.Status, x.StartUtc });

        // En begivenhed med vagter kan ikke slettes (PlanningManager tjekker det, FK'en er sikkerhedsnettet).
        builder.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
    }
}
