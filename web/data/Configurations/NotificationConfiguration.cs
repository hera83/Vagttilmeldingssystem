using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Kind).HasMaxLength(50);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Body).HasMaxLength(1000);
        builder.Property(x => x.Url).HasMaxLength(500);
        builder.Property(x => x.DedupKey).HasMaxLength(200);
        builder.Property(x => x.LastError).HasMaxLength(1000);

        builder.HasIndex(x => x.DedupKey).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
        builder.HasIndex(x => x.UserId);

        // Slettes brugeren, slettes brugerens beskeder med.
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
