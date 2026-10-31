using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpendWise.Data.Entities;

namespace SpendWise.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Username).IsRequired().HasMaxLength(32);
        builder.Property(u => u.NormalizedUsername).IsRequired().HasMaxLength(32);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).IsRequired().HasMaxLength(16);

        builder.HasIndex(u => u.NormalizedUsername).IsUnique();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
    }
}
