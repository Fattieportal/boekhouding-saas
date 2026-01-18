using Boekhouding.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Boekhouding.Infrastructure.Data.Configurations;

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("BankTransactions");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ExternalId)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(e => e.BookingDate)
            .IsRequired();
        
        builder.Property(e => e.Amount)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(e => e.Currency)
            .IsRequired()
            .HasMaxLength(3);
        
        builder.Property(e => e.CounterpartyName)
            .HasMaxLength(200);
        
        builder.Property(e => e.CounterpartyIban)
            .HasMaxLength(50);
        
        builder.Property(e => e.Description)
            .HasMaxLength(500);
        
        builder.Property(e => e.MatchedStatus)
            .IsRequired();
        
        // Unique constraint op TenantId + ExternalId
        builder.HasIndex(e => new { e.TenantId, e.ExternalId })
            .IsUnique();
        
        // Indexes
        builder.HasIndex(e => e.BankConnectionId);
        builder.HasIndex(e => e.BookingDate);
        builder.HasIndex(e => e.MatchedStatus);
        builder.HasIndex(e => e.MatchedInvoiceId);
        
        // Relationships
        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.BankConnection)
            .WithMany(c => c.Transactions)
            .HasForeignKey(e => e.BankConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.MatchedInvoice)
            .WithMany()
            .HasForeignKey(e => e.MatchedInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.JournalEntry)
            .WithMany()
            .HasForeignKey(e => e.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
