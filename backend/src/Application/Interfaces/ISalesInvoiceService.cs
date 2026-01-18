using Boekhouding.Application.DTOs.SalesInvoices;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.Interfaces;

public interface ISalesInvoiceService
{
    Task<IEnumerable<SalesInvoiceDto>> GetAllInvoicesAsync(
        InvoiceStatus? status = null, 
        bool? overdue = null,
        DateTime? from = null,
        DateTime? to = null);
    Task<SalesInvoiceDto?> GetInvoiceByIdAsync(Guid id);
    Task<SalesInvoiceDto> CreateInvoiceAsync(CreateSalesInvoiceDto dto);
    Task<SalesInvoiceDto?> UpdateInvoiceAsync(Guid id, UpdateSalesInvoiceDto dto);
    Task<bool> DeleteInvoiceAsync(Guid id);
    Task<byte[]?> RenderInvoicePdfAsync(Guid id);
    Task<SalesInvoiceDto?> PostInvoiceAsync(Guid id);
}
