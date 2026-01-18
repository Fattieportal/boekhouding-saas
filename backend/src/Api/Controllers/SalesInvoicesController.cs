using Boekhouding.Api.Authorization;
using Boekhouding.Application.DTOs.SalesInvoices;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesInvoicesController : ControllerBase
{
    private readonly ISalesInvoiceService _service;
    private readonly IFileStorage _fileStorage;

    public SalesInvoicesController(ISalesInvoiceService service, IFileStorage fileStorage)
    {
        _service = service;
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Get all invoices with optional filtering
    /// </summary>
    /// <param name="status">Filter by invoice status (0=Draft, 1=Sent, 2=Posted, 3=Paid)</param>
    /// <param name="overdue">Filter overdue invoices only</param>
    /// <param name="from">Filter by issue date from</param>
    /// <param name="to">Filter by issue date to</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesInvoiceDto>>> GetAll(
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] bool? overdue = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var invoices = await _service.GetAllInvoicesAsync(status, overdue, from, to);
        return Ok(invoices);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SalesInvoiceDto>> GetById(Guid id)
    {
        var invoice = await _service.GetInvoiceByIdAsync(id);
        if (invoice == null)
            return NotFound();

        return Ok(invoice);
    }

    [HttpPost]
    public async Task<ActionResult<SalesInvoiceDto>> Create([FromBody] CreateSalesInvoiceDto dto)
    {
        try
        {
            var invoice = await _service.CreateInvoiceAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? "";
            return BadRequest(new { message = ex.Message, innerMessage });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SalesInvoiceDto>> Update(Guid id, [FromBody] UpdateSalesInvoiceDto dto)
    {
        try
        {
            var invoice = await _service.UpdateInvoiceAsync(id, dto);
            if (invoice == null)
                return NotFound();

            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _service.DeleteInvoiceAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/render-pdf")]
    public async Task<ActionResult> RenderPdf(Guid id)
    {
        try
        {
            var pdfBytes = await _service.RenderInvoicePdfAsync(id);
            if (pdfBytes == null)
                return NotFound();

            return File(pdfBytes, "application/pdf", $"invoice_{id}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/download-pdf")]
    public async Task<ActionResult> DownloadPdf(Guid id)
    {
        var invoice = await _service.GetInvoiceByIdAsync(id);
        if (invoice == null)
            return NotFound();

        if (!invoice.PdfFileId.HasValue)
            return NotFound(new { message = "PDF not yet generated. Use /render-pdf endpoint first." });

        try
        {
            var pdfBytes = await _fileStorage.GetFileAsync(invoice.PdfFileId.Value);
            return File(pdfBytes, "application/pdf", $"invoice_{invoice.InvoiceNumber}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/post")]
    [Authorize(Policy = Policies.RequireAdminOrOwner)]
    public async Task<ActionResult<SalesInvoiceDto>> Post(Guid id)
    {
        try
        {
            var invoice = await _service.PostInvoiceAsync(id);
            if (invoice == null)
                return NotFound();

            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
