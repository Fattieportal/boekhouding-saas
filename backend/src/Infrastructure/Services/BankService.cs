using Boekhouding.Application.DTOs.Banking;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class BankService : IBankService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IJournalEntryService _journalEntryService;
    private readonly Dictionary<string, IBankProvider> _providers;

    public BankService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        IJournalEntryService journalEntryService,
        IEnumerable<IBankProvider> providers)
    {
        _context = context;
        _tenantContext = tenantContext;
        _journalEntryService = journalEntryService;
        _providers = providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<BankConnectionInitiateResponse> InitiateConnectionAsync(
        string provider, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is not set");

        if (!_providers.TryGetValue(provider, out var bankProvider))
        {
            throw new ArgumentException($"Provider '{provider}' not found", nameof(provider));
        }

        // Initieer connectie bij provider
        var (externalConnectionId, consentUrl) = await bankProvider.InitiateConnectionAsync(
            tenantId, 
            cancellationToken: cancellationToken);

        // Maak database record
        var connection = new BankConnection
        {
            TenantId = tenantId,
            Provider = provider,
            Status = BankConnectionStatus.Pending,
            ExternalConnectionId = externalConnectionId
        };

        _context.Set<BankConnection>().Add(connection);
        await _context.SaveChangesAsync(cancellationToken);

        return new BankConnectionInitiateResponse
        {
            ConnectionId = connection.Id,
            ConsentUrl = consentUrl
        };
    }

    public async Task<List<BankConnection>> GetConnectionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<BankConnection>()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<BankConnection?> GetConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BankConnection>()
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);
    }

    public async Task<BankSyncResponse> SyncTransactionsAsync(
        Guid connectionId, 
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        var connection = await _context.Set<BankConnection>()
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Connection {connectionId} not found");

        if (connection.Status != BankConnectionStatus.Active && connection.Status != BankConnectionStatus.Pending)
        {
            throw new InvalidOperationException($"Connection is not active (status: {connection.Status})");
        }

        if (!_providers.TryGetValue(connection.Provider, out var provider))
        {
            throw new InvalidOperationException($"Provider '{connection.Provider}' not found");
        }

        // Voor Pending status (eerste sync na consent), activeer de connectie
        if (connection.Status == BankConnectionStatus.Pending)
        {
            // In echte implementatie: complete OAuth flow met authorization code
            // Voor Mock: genereer direct tokens
            var (accessToken, refreshToken, expiresAt) = await provider.CompleteConnectionAsync(
                connection.ExternalConnectionId!,
                "mock_auth_code",
                cancellationToken);

            connection.AccessTokenEncrypted = accessToken;
            connection.RefreshTokenEncrypted = refreshToken;
            connection.ExpiresAt = expiresAt;
            connection.Status = BankConnectionStatus.Active;
            connection.BankName = "Mock Bank";
            connection.IbanMasked = "NL**MOCK****1234";
        }

        // Check of token refresh nodig is
        if (connection.ExpiresAt.HasValue && connection.ExpiresAt.Value <= DateTime.UtcNow.AddDays(7))
        {
            var (newAccessToken, newRefreshToken, newExpiresAt) = await provider.RefreshTokenAsync(
                connection.RefreshTokenEncrypted!,
                cancellationToken);

            connection.AccessTokenEncrypted = newAccessToken;
            connection.RefreshTokenEncrypted = newRefreshToken;
            connection.ExpiresAt = newExpiresAt;
        }

        // Haal transacties op
        var transactions = await provider.SyncTransactionsAsync(
            connection.AccessTokenEncrypted!,
            from,
            to,
            cancellationToken);

        // Upsert transacties
        int imported = 0;
        int updated = 0;

        foreach (var txDto in transactions)
        {
            var existing = await _context.Set<BankTransaction>()
                .FirstOrDefaultAsync(t => 
                    t.BankConnectionId == connectionId && 
                    t.ExternalId == txDto.ExternalId, 
                    cancellationToken);

            if (existing == null)
            {
                var newTx = new BankTransaction
                {
                    TenantId = connection.TenantId,
                    BankConnectionId = connectionId,
                    ExternalId = txDto.ExternalId,
                    BookingDate = txDto.BookingDate,
                    ValueDate = txDto.ValueDate,
                    Amount = txDto.Amount,
                    Currency = txDto.Currency,
                    CounterpartyName = txDto.CounterpartyName,
                    CounterpartyIban = txDto.CounterpartyIban,
                    Description = txDto.Description,
                    MatchedStatus = BankTransactionMatchStatus.Unmatched
                };

                _context.Set<BankTransaction>().Add(newTx);
                imported++;
            }
            else
            {
                // Update indien nodig (bijvoorbeeld status updates)
                existing.CounterpartyName = txDto.CounterpartyName;
                existing.Description = txDto.Description;
                updated++;
            }
        }

        connection.LastSyncedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new BankSyncResponse
        {
            TransactionsImported = imported,
            TransactionsUpdated = updated,
            SyncedAt = connection.LastSyncedAt.Value
        };
    }

    public async Task<List<BankTransaction>> GetTransactionsAsync(
        Guid? connectionId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<BankTransaction>()
            .Include(t => t.BankConnection)
            .Include(t => t.MatchedInvoice)
            .AsQueryable();

        if (connectionId.HasValue)
        {
            query = query.Where(t => t.BankConnectionId == connectionId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(t => t.BookingDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.BookingDate <= to.Value);
        }

        return await query
            .OrderByDescending(t => t.BookingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task MatchTransactionToInvoiceAsync(
        Guid transactionId, 
        Guid invoiceId, 
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Set<BankTransaction>()
            .Include(t => t.BankConnection)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Transaction {transactionId} not found");

        var invoice = await _context.Set<SalesInvoice>()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found");

        if (transaction.MatchedStatus != BankTransactionMatchStatus.Unmatched)
        {
            throw new InvalidOperationException("Transaction is already matched");
        }
        
        // Validatie: invoice must have open amount
        if (invoice.OpenAmount <= 0)
        {
            throw new InvalidOperationException($"Invoice {invoice.InvoiceNumber} is already fully paid");
        }
        
        // Validatie: invoice must be Posted or Sent (not Draft)
        if (invoice.Status == InvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot match payments to draft invoices. Post invoice {invoice.InvoiceNumber} first.");
        }

        // Validatie: bedrag moet positief zijn (inkomende betaling)
        if (transaction.Amount <= 0)
        {
            throw new InvalidOperationException("Can only match credit transactions to invoices");
        }

        // Haal Bank en Debiteuren accounts op
        var bankAccount = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == "1010", cancellationToken)
            ?? throw new InvalidOperationException("Bank account (1010) not found");

        var debtorsAccount = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == "1300", cancellationToken)
            ?? throw new InvalidOperationException("Debtors account (1300) not found");

        var bankJournal = await _context.Set<Journal>()
            .FirstOrDefaultAsync(j => j.Code == "BANK", cancellationToken)
            ?? throw new InvalidOperationException("Bank journal not found");

        // Maak journal entry
        var journalEntry = new JournalEntry
        {
            TenantId = transaction.TenantId,
            JournalId = bankJournal.Id,
            EntryDate = transaction.BookingDate,
            Reference = invoice.InvoiceNumber,
            Description = $"Payment for invoice {invoice.InvoiceNumber} - {transaction.CounterpartyName}",
            Status = JournalEntryStatus.Draft
        };

        var lines = new List<JournalLine>
        {
            // Debit Bank
            new()
            {
                TenantId = transaction.TenantId,
                AccountId = bankAccount.Id,
                Description = $"Payment {invoice.InvoiceNumber}",
                Debit = transaction.Amount,
                Credit = 0
            },
            // Credit Debiteuren
            new()
            {
                TenantId = transaction.TenantId,
                AccountId = debtorsAccount.Id,
                Description = $"Payment {invoice.InvoiceNumber}",
                Debit = 0,
                Credit = transaction.Amount
            }
        };

        _context.Set<JournalEntry>().Add(journalEntry);
        await _context.SaveChangesAsync(cancellationToken);

        // Voeg lines toe
        foreach (var line in lines)
        {
            line.EntryId = journalEntry.Id;
            _context.Set<JournalLine>().Add(line);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Post de entry
        await _journalEntryService.PostEntryAsync(journalEntry.Id, cancellationToken);

        // Update transaction
        transaction.MatchedInvoiceId = invoiceId;
        transaction.MatchedStatus = BankTransactionMatchStatus.MatchedToInvoice;
        transaction.JournalEntryId = journalEntry.Id;
        transaction.MatchedAt = DateTime.UtcNow;

        // Update invoice OpenAmount and Status
        invoice.OpenAmount -= transaction.Amount; // Reduce open amount by payment
        if (invoice.OpenAmount <= 0.01m) // Account for rounding
        {
            invoice.OpenAmount = 0;
            invoice.Status = InvoiceStatus.Paid;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _context.Set<BankConnection>()
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Connection {connectionId} not found");

        // Verwijder eerst alle transacties
        _context.Set<BankTransaction>().RemoveRange(connection.Transactions);
        
        // Verwijder de connectie
        _context.Set<BankConnection>().Remove(connection);
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
