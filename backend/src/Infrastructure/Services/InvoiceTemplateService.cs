using Boekhouding.Application.DTOs.InvoiceTemplates;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class InvoiceTemplateService : IInvoiceTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public InvoiceTemplateService(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<InvoiceTemplateDto>> GetAllTemplatesAsync()
    {
        return await _context.Set<InvoiceTemplate>()
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<InvoiceTemplateDto?> GetTemplateByIdAsync(Guid id)
    {
        var template = await _context.Set<InvoiceTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id);

        return template != null ? MapToDto(template) : null;
    }

    public async Task<InvoiceTemplateDto?> GetDefaultTemplateAsync()
    {
        var template = await _context.Set<InvoiceTemplate>()
            .FirstOrDefaultAsync(t => t.IsDefault);

        return template != null ? MapToDto(template) : null;
    }

    public async Task<InvoiceTemplateDto> CreateTemplateAsync(CreateInvoiceTemplateDto dto)
    {
        var template = new InvoiceTemplate
        {
            Name = dto.Name,
            IsDefault = dto.IsDefault,
            HtmlTemplate = dto.HtmlTemplate,
            CssTemplate = dto.CssTemplate,
            SettingsJson = dto.SettingsJson
        };

        // Als deze template default is, maak andere templates non-default
        if (template.IsDefault)
        {
            await UnsetOtherDefaultsAsync();
        }

        _context.Set<InvoiceTemplate>().Add(template);
        await _context.SaveChangesAsync();

        return MapToDto(template);
    }

    public async Task<InvoiceTemplateDto?> UpdateTemplateAsync(Guid id, UpdateInvoiceTemplateDto dto)
    {
        var template = await _context.Set<InvoiceTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return null;

        if (dto.Name != null) template.Name = dto.Name;
        if (dto.HtmlTemplate != null) template.HtmlTemplate = dto.HtmlTemplate;
        if (dto.CssTemplate != null) template.CssTemplate = dto.CssTemplate;
        if (dto.SettingsJson != null) template.SettingsJson = dto.SettingsJson;
        
        if (dto.IsDefault.HasValue && dto.IsDefault.Value && !template.IsDefault)
        {
            await UnsetOtherDefaultsAsync();
            template.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue)
        {
            template.IsDefault = dto.IsDefault.Value;
        }

        await _context.SaveChangesAsync();

        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        var template = await _context.Set<InvoiceTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return false;

        _context.Set<InvoiceTemplate>().Remove(template);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<InvoiceTemplateDto?> SetDefaultTemplateAsync(Guid id)
    {
        var template = await _context.Set<InvoiceTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return null;

        await UnsetOtherDefaultsAsync();
        template.IsDefault = true;
        await _context.SaveChangesAsync();

        return MapToDto(template);
    }

    private async Task UnsetOtherDefaultsAsync()
    {
        var defaults = await _context.Set<InvoiceTemplate>()
            .Where(t => t.IsDefault)
            .ToListAsync();

        foreach (var t in defaults)
        {
            t.IsDefault = false;
        }
    }

    private static InvoiceTemplateDto MapToDto(InvoiceTemplate template)
    {
        return new InvoiceTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            IsDefault = template.IsDefault,
            HtmlTemplate = template.HtmlTemplate,
            CssTemplate = template.CssTemplate,
            SettingsJson = template.SettingsJson,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
