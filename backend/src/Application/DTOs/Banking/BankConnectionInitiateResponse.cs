namespace Boekhouding.Application.DTOs.Banking;

/// <summary>
/// Response voor bank connectie initiatie
/// </summary>
public class BankConnectionInitiateResponse
{
    public Guid ConnectionId { get; set; }
    public string ConsentUrl { get; set; } = string.Empty;
}
