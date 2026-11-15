using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(10);

        // En bruger kan højst have én aktiv tildeling pr. vagt. Delvist index: afmeldte rækker tæller ikke.
        builder.HasIndex(x => new { x.ShiftId, x.UserId }).IsUnique().HasFilter("\"IsActive\" = 1");
        builder.HasIndex(x => new { x.ShiftId, x.IsActive });
        builder.HasIndex(x => new { x.UserId, x.IsActive });

        // Slettes en vagt (kun tilladt uden aktive tildelinger), slettes dens afmeldte historik med.
        builder.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Cascade);

        // En bruger med tildelinger kan ikke slettes (MemberManager tjekker det, FK'en er sikkerhedsnettet).
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
