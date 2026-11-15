using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // NOCASE: brugernavne er unikke og slås op uden forskel på store og små bogstaver.
        builder.Property(x => x.UserName).HasMaxLength(100).UseCollation("NOCASE");
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.PasswordHash).HasMaxLength(500);
        builder.HasIndex(x => x.UserName).IsUnique();
    }
}
