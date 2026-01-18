using Boekhouding.Api.Authorization;
using Boekhouding.Application.DTOs.Reports;
using Boekhouding.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Haal openstaande debiteuren op per klant
    /// </summary>
    [HttpGet("ar")]
    [Authorize(Policy = Policies.RequireAdminOrOwner)]
    public async Task<ActionResult<List<AccountsReceivableDto>>> GetAccountsReceivable()
    {
        try
        {
            var result = await _reportService.GetAccountsReceivableAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Haal BTW rapportage op voor een periode
    /// </summary>
    /// <param name="from">Start datum (YYYY-MM-DD)</param>
    /// <param name="to">Eind datum (YYYY-MM-DD)</param>
    /// <returns>BTW rapportage met omzet en BTW per tarief</returns>
    /// <response code="200">BTW rapportage succesvol opgehaald</response>
    /// <response code="400">Ongeldige datums</response>
    [HttpGet("vat")]
    [Authorize(Policy = Policies.RequireAdminOrOwner)]
    [ProducesResponseType(typeof(VatReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VatReportDto>> GetVatReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        try
        {
            if (from > to)
            {
                return BadRequest(new { message = "From date must be before or equal to To date" });
            }

            var result = await _reportService.GetVatReportAsync(from, to);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
