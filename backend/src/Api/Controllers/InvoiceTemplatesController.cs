using Boekhouding.Api.Authorization;
using Boekhouding.Application.DTOs.InvoiceTemplates;
using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceTemplatesController : ControllerBase
{
    private readonly IInvoiceTemplateService _service;

    public InvoiceTemplatesController(IInvoiceTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceTemplateDto>>> GetAll()
    {
        var templates = await _service.GetAllTemplatesAsync();
        return Ok(templates);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceTemplateDto>> GetById(Guid id)
    {
        var template = await _service.GetTemplateByIdAsync(id);
        if (template == null)
            return NotFound();

        return Ok(template);
    }

    [HttpGet("default")]
    public async Task<ActionResult<InvoiceTemplateDto>> GetDefault()
    {
        var template = await _service.GetDefaultTemplateAsync();
        if (template == null)
            return NotFound(new { message = "No default template found" });

        return Ok(template);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdminOrOwner)]
    public async Task<ActionResult<InvoiceTemplateDto>> Create([FromBody] CreateInvoiceTemplateDto dto)
    {
        var template = await _service.CreateTemplateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.RequireAdminOrOwner)]
    public async Task<ActionResult<InvoiceTemplateDto>> Update(Guid id, [FromBody] UpdateInvoiceTemplateDto dto)
    {
        var template = await _service.UpdateTemplateAsync(id, dto);
        if (template == null)
            return NotFound();

        return Ok(template);
    }

    [HttpPost("{id}/set-default")]
    [Authorize(Policy = Policies.RequireAdminOrOwner)]
    public async Task<ActionResult<InvoiceTemplateDto>> SetDefault(Guid id)
    {
        var template = await _service.SetDefaultTemplateAsync(id);
        if (template == null)
            return NotFound();

        return Ok(template);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireAdminOrOwner)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteTemplateAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
