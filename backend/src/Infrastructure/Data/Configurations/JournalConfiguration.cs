using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Boekhouding.Infrastructure.Data.Configurations;

public class JournalConfiguration : IEntityTypeConfiguration<Journal>
{
    public void Configure(EntityTypeBuilder<Journal> builder)
    {
        builder.ToTable("Journals");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(j => j.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(j => j.CreatedAt)
            .IsRequired();

        // Unique constraint: Code moet uniek zijn per Tenant
        builder.HasIndex(j => new { j.TenantId, j.Code })
            .IsUnique()
            .HasDatabaseName("IX_Journals_TenantId_Code");

        // Index voor type queries
        builder.HasIndex(j => new { j.TenantId, j.Type })
            .HasDatabaseName("IX_Journals_TenantId_Type");

        // Relationships
        builder.HasOne(j => j.Tenant)
            .WithMany()
            .HasForeignKey(j => j.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
