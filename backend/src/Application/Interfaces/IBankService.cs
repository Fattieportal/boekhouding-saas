using Boekhouding.Application.DTOs.Banking;

namespace Boekhouding.Application.Interfaces;

public interface IBankService
{
    /// <summary>
    /// Start een nieuwe bank connectie
    /// </summary>
    Task<BankConnectionInitiateResponse> InitiateConnectionAsync(string provider, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Haal alle bank connecties op voor de huidige tenant
    /// </summary>
    Task<List<Domain.Entities.BankConnection>> GetConnectionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Haal een specifieke bank connectie op
    /// </summary>
    Task<Domain.Entities.BankConnection?> GetConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sync transacties voor een connectie
    /// </summary>
    Task<BankSyncResponse> SyncTransactionsAsync(Guid connectionId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Haal transacties op
    /// </summary>
    Task<List<Domain.Entities.BankTransaction>> GetTransactionsAsync(
        Guid? connectionId = null, 
        DateTime? from = null, 
        DateTime? to = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Match een transactie met een factuur
    /// </summary>
    Task MatchTransactionToInvoiceAsync(Guid transactionId, Guid invoiceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unmatch een transactie van een factuur (reverse matching)
    /// </summary>
    Task UnmatchTransactionAsync(Guid transactionId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reconcile (afsluiten) van bank transacties voor een periode
    /// </summary>
    Task<BankReconciliationResponse> ReconcileTransactionsAsync(
        Guid connectionId, 
        DateTime periodStart, 
        DateTime periodEnd,
        decimal openingBalance,
        decimal closingBalance,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verwijder een bank connectie
    /// </summary>
    Task DeleteConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default);
}
