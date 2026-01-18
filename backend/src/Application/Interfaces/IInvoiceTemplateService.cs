using Boekhouding.Application.DTOs.InvoiceTemplates;

namespace Boekhouding.Application.Interfaces;

public interface IInvoiceTemplateService
{
    Task<IEnumerable<InvoiceTemplateDto>> GetAllTemplatesAsync();
    Task<InvoiceTemplateDto?> GetTemplateByIdAsync(Guid id);
    Task<InvoiceTemplateDto?> GetDefaultTemplateAsync();
    Task<InvoiceTemplateDto> CreateTemplateAsync(CreateInvoiceTemplateDto dto);
    Task<InvoiceTemplateDto?> UpdateTemplateAsync(Guid id, UpdateInvoiceTemplateDto dto);
    Task<bool> DeleteTemplateAsync(Guid id);
    Task<InvoiceTemplateDto?> SetDefaultTemplateAsync(Guid id);
}
