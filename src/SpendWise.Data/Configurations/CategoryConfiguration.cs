using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpendWise.Data.Entities;

namespace SpendWise.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(40);
        builder.Property(c => c.Color).IsRequired().HasMaxLength(7);
        builder.Ignore(c => c.IsGlobal);

        // Name uniqueness spans global (UserId = null) and personal categories,
        // which one index can't express, so CategoryService enforces it.
        builder.HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
