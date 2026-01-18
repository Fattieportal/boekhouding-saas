namespace Boekhouding.Application.DTOs.Banking;

/// <summary>
/// Response na bank transacties sync
/// </summary>
public class BankSyncResponse
{
    public int TransactionsImported { get; set; }
    public int TransactionsUpdated { get; set; }
    public DateTime SyncedAt { get; set; }
}
