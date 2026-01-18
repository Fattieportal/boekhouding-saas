using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Boekhouding.Infrastructure.Data.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.AddressLine1)
            .HasMaxLength(200);

        builder.Property(c => c.AddressLine2)
            .HasMaxLength(200);

        builder.Property(c => c.PostalCode)
            .HasMaxLength(20);

        builder.Property(c => c.City)
            .HasMaxLength(100);

        builder.Property(c => c.Country)
            .IsRequired()
            .HasMaxLength(2)
            .HasDefaultValue("NL");

        builder.Property(c => c.VatNumber)
            .HasMaxLength(50);

        builder.Property(c => c.KvK)
            .HasMaxLength(50);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Index voor snelle zoekopdrachten
        builder.HasIndex(c => new { c.TenantId, c.DisplayName })
            .HasDatabaseName("IX_Contacts_TenantId_DisplayName");

        builder.HasIndex(c => new { c.TenantId, c.Type, c.IsActive })
            .HasDatabaseName("IX_Contacts_TenantId_Type_IsActive");

        builder.HasIndex(c => new { c.TenantId, c.Email })
            .HasDatabaseName("IX_Contacts_TenantId_Email");

        // Relationships
        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
