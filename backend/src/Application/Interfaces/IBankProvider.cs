namespace Boekhouding.Application.Interfaces;

public interface IBankProvider
{
    /// <summary>
    /// Provider naam
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Start een bank connectie flow en retourneer consent URL
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="redirectUrl">Redirect URL na consent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple met External Connection ID en Consent URL</returns>
    Task<(string ExternalConnectionId, string ConsentUrl)> InitiateConnectionAsync(
        Guid tenantId, 
        string? redirectUrl = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Voltooi de connectie na consent callback
    /// </summary>
    /// <param name="externalConnectionId">External connection ID</param>
    /// <param name="authorizationCode">Authorization code uit callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple met encrypted access token, refresh token en expiry</returns>
    Task<(string AccessTokenEncrypted, string RefreshTokenEncrypted, DateTime ExpiresAt)> CompleteConnectionAsync(
        string externalConnectionId,
        string authorizationCode,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Haal transacties op voor een periode
    /// </summary>
    /// <param name="accessTokenEncrypted">Encrypted access token</param>
    /// <param name="from">Begin datum</param>
    /// <param name="to">Eind datum</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lijst van bank transacties</returns>
    Task<List<DTOs.Banking.BankTransactionDto>> SyncTransactionsAsync(
        string accessTokenEncrypted,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refresh de access token
    /// </summary>
    /// <param name="refreshTokenEncrypted">Encrypted refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Nieuwe encrypted tokens en expiry</returns>
    Task<(string AccessTokenEncrypted, string RefreshTokenEncrypted, DateTime ExpiresAt)> RefreshTokenAsync(
        string refreshTokenEncrypted,
        CancellationToken cancellationToken = default);
}
