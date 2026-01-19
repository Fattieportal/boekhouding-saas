using Boekhouding.Application.DTOs.VAT;

namespace Boekhouding.Application.Interfaces;

public interface IVATService
{
    /// <summary>
    /// Calculate VAT for a period
    /// </summary>
    Task<VATCalculationResponse> CalculateVATAsync(int year, int quarter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Submit VAT return to tax authority
    /// </summary>
    Task<VATSubmissionResponse> SubmitVATAsync(Guid calculationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Approve calculated VAT return
    /// </summary>
    Task<bool> ApproveVATAsync(Guid calculationId, string approvedBy, CancellationToken cancellationToken = default);
}
