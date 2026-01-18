using Boekhouding.Application.DTOs.Banking;
using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Boekhouding.Infrastructure.Services;

/// <summary>
/// Mock bank provider voor development en testing
/// </summary>
public class MockBankProvider : IBankProvider
{
    private readonly IDataProtector _dataProtector;
    private readonly Random _random = new();

    public MockBankProvider(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("BankProvider.Mock");
    }

    public string ProviderName => "Mock";

    public Task<(string ExternalConnectionId, string ConsentUrl)> InitiateConnectionAsync(
        Guid tenantId, 
        string? redirectUrl = null,
        CancellationToken cancellationToken = default)
    {
        var externalConnectionId = $"mock_conn_{Guid.NewGuid():N}";
        var consentUrl = $"https://mock-bank.example.com/consent?connection={externalConnectionId}&redirect={Uri.EscapeDataString(redirectUrl ?? "http://localhost:3000/banking/callback")}";
        
        return Task.FromResult((externalConnectionId, consentUrl));
    }

    public Task<(string AccessTokenEncrypted, string RefreshTokenEncrypted, DateTime ExpiresAt)> CompleteConnectionAsync(
        string externalConnectionId,
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        // In een echte implementatie zou hier een OAuth token exchange plaatsvinden
        var accessToken = $"mock_access_{Guid.NewGuid():N}";
        var refreshToken = $"mock_refresh_{Guid.NewGuid():N}";
        
        var accessTokenEncrypted = _dataProtector.Protect(accessToken);
        var refreshTokenEncrypted = _dataProtector.Protect(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(90);
        
        return Task.FromResult((accessTokenEncrypted, refreshTokenEncrypted, expiresAt));
    }

    public Task<List<BankTransactionDto>> SyncTransactionsAsync(
        string accessTokenEncrypted,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        // Voor mock: token decryptie is niet nodig, we gebruiken mock data
        // In een echte provider zou je hier de API aanroepen met de gedecodeerde token
        
        // Genereer mock transacties
        var transactions = new List<BankTransactionDto>();
        var currentDate = from;
        
        while (currentDate <= to)
        {
            // 20% kans op transactie per dag
            if (_random.Next(100) < 20)
            {
                var transaction = GenerateMockTransaction(currentDate);
                transactions.Add(transaction);
            }
            
            currentDate = currentDate.AddDays(1);
        }
        
        return Task.FromResult(transactions);
    }

    public Task<(string AccessTokenEncrypted, string RefreshTokenEncrypted, DateTime ExpiresAt)> RefreshTokenAsync(
        string refreshTokenEncrypted,
        CancellationToken cancellationToken = default)
    {
        // Voor mock: we genereren gewoon nieuwe tokens zonder de oude te decrypten
        
        // Genereer nieuwe tokens
        var newAccessToken = $"mock_access_{Guid.NewGuid():N}";
        var newRefreshToken = $"mock_refresh_{Guid.NewGuid():N}";
        
        var newAccessTokenEncrypted = _dataProtector.Protect(newAccessToken);
        var newRefreshTokenEncrypted = _dataProtector.Protect(newRefreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(90);
        
        return Task.FromResult((newAccessTokenEncrypted, newRefreshTokenEncrypted, expiresAt));
    }

    private BankTransactionDto GenerateMockTransaction(DateTime date)
    {
        // Ensure dates are UTC for PostgreSQL compatibility
        var bookingDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        
        var isCredit = _random.Next(2) == 0;
        var amount = isCredit 
            ? Math.Round((decimal)(_random.NextDouble() * 5000), 2)
            : -Math.Round((decimal)(_random.NextDouble() * 2000), 2);
        
        var counterparties = new[]
        {
            ("Acme Corp", "NL91ABNA0417164300"),
            ("XYZ Solutions", "NL20INGB0001234567"),
            ("Tech Innovators BV", "NL39RABO0300065264"),
            ("Green Energy Ltd", "NL13TRIO0338418752"),
            ("Digital Services", "NL43ASNB0267651217"),
            ("Consultancy Partners", "NL56BUNQ2025848484"),
            ("Marketing Experts", "NL81KNAB0255182588"),
            ("Office Supplies Co", "NL92SNSB0921075456")
        };
        
        var (counterpartyName, counterpartyIban) = counterparties[_random.Next(counterparties.Length)];
        
        var descriptions = isCredit
            ? new[] { "Payment invoice", "Customer payment", "Transfer received", "Refund" }
            : new[] { "Supplier payment", "Subscription fee", "Office expenses", "Service payment" };
        
        return new BankTransactionDto
        {
            ExternalId = $"mock_tx_{Guid.NewGuid():N}",
            BookingDate = bookingDate,
            ValueDate = bookingDate.AddDays(_random.Next(0, 3)),
            Amount = amount,
            Currency = "EUR",
            CounterpartyName = counterpartyName,
            CounterpartyIban = counterpartyIban,
            Description = descriptions[_random.Next(descriptions.Length)]
        };
    }
}
