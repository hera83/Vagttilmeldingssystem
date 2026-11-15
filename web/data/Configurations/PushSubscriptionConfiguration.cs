using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.Property(x => x.Endpoint).HasMaxLength(2048);
        builder.Property(x => x.P256dh).HasMaxLength(200);
        builder.Property(x => x.Auth).HasMaxLength(100);
        builder.HasIndex(x => x.Endpoint).IsUnique();
        builder.HasIndex(x => x.UserId);

        // Slettes brugeren, slettes enhedernes abonnementer med.
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
