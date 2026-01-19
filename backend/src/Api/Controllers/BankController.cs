using Boekhouding.Application.DTOs.Banking;
using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BankController : ControllerBase
{
    private readonly IBankService _bankService;

    public BankController(IBankService bankService)
    {
        _bankService = bankService;
    }

    /// <summary>
    /// Start een nieuwe bank connectie
    /// </summary>
    [HttpPost("connect")]
    public async Task<IActionResult> InitiateConnection(
        [FromBody] InitiateConnectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _bankService.InitiateConnectionAsync(
                request.Provider, 
                cancellationToken);
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Haal alle bank connecties op
    /// </summary>
    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections(CancellationToken cancellationToken)
    {
        var connections = await _bankService.GetConnectionsAsync(cancellationToken);
        
        var response = connections.Select(c => new
        {
            c.Id,
            c.Provider,
            c.Status,
            c.BankName,
            c.IbanMasked,
            c.LastSyncedAt,
            c.ExpiresAt,
            c.CreatedAt
        });
        
        return Ok(response);
    }

    /// <summary>
    /// Haal een specifieke connectie op
    /// </summary>
    [HttpGet("connections/{connectionId}")]
    public async Task<IActionResult> GetConnection(Guid connectionId, CancellationToken cancellationToken)
    {
        var connection = await _bankService.GetConnectionAsync(connectionId, cancellationToken);
        
        if (connection == null)
        {
            return NotFound();
        }
        
        return Ok(new
        {
            connection.Id,
            connection.Provider,
            connection.Status,
            connection.BankName,
            connection.IbanMasked,
            connection.LastSyncedAt,
            connection.ExpiresAt,
            connection.CreatedAt
        });
    }

    /// <summary>
    /// Sync transacties voor een connectie
    /// </summary>
    [HttpPost("connections/{connectionId}/sync")]
    public async Task<IActionResult> SyncTransactions(
        Guid connectionId,
        [FromBody] SyncTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var from = request.From ?? DateTime.UtcNow.AddMonths(-1);
            var to = request.To ?? DateTime.UtcNow;
            
            var response = await _bankService.SyncTransactionsAsync(
                connectionId, 
                from, 
                to, 
                cancellationToken);
            
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Haal transacties op
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid? connectionId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var transactions = await _bankService.GetTransactionsAsync(
            connectionId, 
            from, 
            to, 
            cancellationToken);
        
        var response = transactions.Select(t => new BankTransactionFullDto
        {
            Id = t.Id,
            BankConnectionId = t.BankConnectionId,
            BankName = t.BankConnection?.BankName ?? string.Empty,
            ExternalId = t.ExternalId,
            BookingDate = t.BookingDate,
            ValueDate = t.ValueDate,
            Amount = t.Amount,
            Currency = t.Currency,
            CounterpartyName = t.CounterpartyName,
            CounterpartyIban = t.CounterpartyIban,
            Description = t.Description,
            MatchedStatus = t.MatchedStatus,
            MatchedInvoiceId = t.MatchedInvoiceId,
            InvoiceNumber = t.MatchedInvoice?.InvoiceNumber,
            JournalEntryId = t.JournalEntryId,
            MatchedAt = t.MatchedAt
        });
        
        return Ok(response);
    }

    /// <summary>
    /// Match een transactie met een factuur
    /// </summary>
    [HttpPost("transactions/{transactionId}/match")]
    public async Task<IActionResult> MatchTransaction(
        Guid transactionId,
        [FromBody] MatchTransactionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _bankService.MatchTransactionToInvoiceAsync(
                transactionId, 
                request.InvoiceId, 
                cancellationToken);
            
            return Ok(new { message = "Transaction matched successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Unmatch een transactie van een factuur
    /// </summary>
    [HttpPost("transactions/{transactionId}/unmatch")]
    public async Task<IActionResult> UnmatchTransaction(
        Guid transactionId,
        [FromBody] UnmatchTransactionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _bankService.UnmatchTransactionAsync(
                transactionId, 
                request.Reason, 
                cancellationToken);
            
            return Ok(new { message = "Transaction unmatched successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reconcile bank transacties voor een periode
    /// </summary>
    [HttpPost("connections/{connectionId}/reconcile")]
    public async Task<IActionResult> ReconcileTransactions(
        Guid connectionId,
        [FromBody] ReconcileTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _bankService.ReconcileTransactionsAsync(
                connectionId,
                request.PeriodStart,
                request.PeriodEnd,
                request.OpeningBalance,
                request.ClosingBalance,
                cancellationToken);
            
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Verwijder een bank connectie
    /// </summary>
    [HttpDelete("connections/{connectionId}")]
    public async Task<IActionResult> DeleteConnection(
        Guid connectionId, 
        CancellationToken cancellationToken)
    {
        try
        {
            await _bankService.DeleteConnectionAsync(connectionId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

// Request models
public record InitiateConnectionRequest(string Provider);
public record SyncTransactionsRequest(DateTime? From, DateTime? To);
public record MatchTransactionRequest(Guid InvoiceId);
public record UnmatchTransactionRequest(string Reason);
public record ReconcileTransactionsRequest(
    DateTime PeriodStart, 
    DateTime PeriodEnd, 
    decimal OpeningBalance, 
    decimal ClosingBalance);
