using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Boekhouding.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Unique constraint: Code moet uniek zijn per Tenant
        builder.HasIndex(a => new { a.TenantId, a.Code })
            .IsUnique()
            .HasDatabaseName("IX_Accounts_TenantId_Code");

        // Index voor type queries
        builder.HasIndex(a => new { a.TenantId, a.Type, a.IsActive })
            .HasDatabaseName("IX_Accounts_TenantId_Type_IsActive");

        // Relationships
        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
