using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Boekhouding.Infrastructure.Data.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(je => je.Id);

        builder.Property(je => je.EntryDate)
            .IsRequired();

        builder.Property(je => je.Reference)
            .HasMaxLength(100);

        builder.Property(je => je.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(je => je.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(je => je.PostedAt);

        builder.Property(je => je.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(je => new { je.TenantId, je.JournalId, je.EntryDate })
            .HasDatabaseName("IX_JournalEntries_TenantId_JournalId_EntryDate");

        builder.HasIndex(je => new { je.TenantId, je.Status, je.EntryDate })
            .HasDatabaseName("IX_JournalEntries_TenantId_Status_EntryDate");

        builder.HasIndex(je => new { je.TenantId, je.Reference })
            .HasDatabaseName("IX_JournalEntries_TenantId_Reference");

        // Relationships
        builder.HasOne(je => je.Tenant)
            .WithMany()
            .HasForeignKey(je => je.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(je => je.Journal)
            .WithMany()
            .HasForeignKey(je => je.JournalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(je => je.Lines)
            .WithOne(jl => jl.Entry)
            .HasForeignKey(jl => jl.EntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing relationship voor reversal
        builder.HasOne(je => je.ReversalOfEntry)
            .WithOne(je => je.ReversedByEntry)
            .HasForeignKey<JournalEntry>(je => je.ReversalOfEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
