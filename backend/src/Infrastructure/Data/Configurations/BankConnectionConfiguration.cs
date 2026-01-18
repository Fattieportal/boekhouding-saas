using Boekhouding.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Boekhouding.Infrastructure.Data.Configurations;

public class BankConnectionConfiguration : IEntityTypeConfiguration<BankConnection>
{
    public void Configure(EntityTypeBuilder<BankConnection> builder)
    {
        builder.ToTable("BankConnections");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(e => e.Status)
            .IsRequired();
        
        builder.Property(e => e.AccessTokenEncrypted)
            .HasMaxLength(2000);
        
        builder.Property(e => e.RefreshTokenEncrypted)
            .HasMaxLength(2000);
        
        builder.Property(e => e.ExternalConnectionId)
            .HasMaxLength(200);
        
        builder.Property(e => e.BankName)
            .HasMaxLength(200);
        
        builder.Property(e => e.IbanMasked)
            .HasMaxLength(50);
        
        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.Provider });
        builder.HasIndex(e => e.ExternalConnectionId);
        
        // Relationships
        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.Transactions)
            .WithOne(t => t.BankConnection)
            .HasForeignKey(t => t.BankConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
