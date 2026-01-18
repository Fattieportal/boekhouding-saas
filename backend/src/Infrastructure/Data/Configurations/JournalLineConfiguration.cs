using Boekhouding.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Boekhouding.Infrastructure.Data.Configurations;

public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("JournalLines");

        builder.HasKey(jl => jl.Id);

        builder.Property(jl => jl.Description)
            .HasMaxLength(500);

        builder.Property(jl => jl.Debit)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(jl => jl.Credit)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(jl => jl.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(jl => new { jl.TenantId, jl.EntryId })
            .HasDatabaseName("IX_JournalLines_TenantId_EntryId");

        builder.HasIndex(jl => new { jl.TenantId, jl.AccountId })
            .HasDatabaseName("IX_JournalLines_TenantId_AccountId");

        // Relationships
        builder.HasOne(jl => jl.Tenant)
            .WithMany()
            .HasForeignKey(jl => jl.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(jl => jl.Entry)
            .WithMany(je => je.Lines)
            .HasForeignKey(jl => jl.EntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(jl => jl.Account)
            .WithMany()
            .HasForeignKey(jl => jl.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
