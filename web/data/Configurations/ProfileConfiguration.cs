using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable(t =>
        {
            // Tivolibilletter: højst 2 ark (krav K04). Håndhæves også af databasen.
            t.HasCheckConstraint("CK_Profiles_TivoliSheets", "\"TivoliSheets\" BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_Profiles_DrinkTickets", "\"DrinkTickets\" BETWEEN 0 AND 50");
        });

        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.ShirtSize).HasMaxLength(10);
        builder.Property(x => x.ChildName).HasMaxLength(200);
        builder.Property(x => x.ChildClass).HasMaxLength(20);

        // Standard til (også for eksisterende profiler). Sentinel true: EF sender kun værdien, når den er false.
        builder.Property(x => x.EmailNotifications).HasDefaultValue(true).HasSentinel(true);

        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne<User>().WithOne().HasForeignKey<Profile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
