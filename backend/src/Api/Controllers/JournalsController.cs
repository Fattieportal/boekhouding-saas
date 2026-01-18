using Boekhouding.Application.DTOs.Journals;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JournalsController : ControllerBase
{
    private readonly IJournalService _journalService;
    private readonly ILogger<JournalsController> _logger;

    public JournalsController(
        IJournalService journalService,
        ILogger<JournalsController> logger)
    {
        _journalService = journalService;
        _logger = logger;
    }

    /// <summary>
    /// Haal alle journals op met paginering en filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetJournals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] JournalType? type = null)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Ongeldige paginering parameters" });
            }

            var (items, totalCount) = await _journalService.GetJournalsAsync(
                page, pageSize, search, type);

            return Ok(new
            {
                items,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij ophalen journals");
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Haal een specifieke journal op via ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<JournalDto>> GetJournal(Guid id)
    {
        try
        {
            var journal = await _journalService.GetJournalByIdAsync(id);
            if (journal == null)
            {
                return NotFound(new { message = "Journal niet gevonden" });
            }

            return Ok(journal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij ophalen journal {JournalId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Haal een journal op via code
    /// </summary>
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<JournalDto>> GetJournalByCode(string code)
    {
        try
        {
            var journal = await _journalService.GetJournalByCodeAsync(code);
            if (journal == null)
            {
                return NotFound(new { message = "Journal niet gevonden" });
            }

            return Ok(journal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij ophalen journal met code {Code}", code);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Maak een nieuwe journal aan
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAccountantOrAdmin")]
    public async Task<ActionResult<JournalDto>> CreateJournal([FromBody] CreateJournalDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var journal = await _journalService.CreateJournalAsync(dto);
            return CreatedAtAction(nameof(GetJournal), new { id = journal.Id }, journal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij aanmaken journal");
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Werk een bestaande journal bij
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAccountantOrAdmin")]
    public async Task<ActionResult<JournalDto>> UpdateJournal(Guid id, [FromBody] UpdateJournalDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var journal = await _journalService.UpdateJournalAsync(id, dto);
            if (journal == null)
            {
                return NotFound(new { message = "Journal niet gevonden" });
            }

            return Ok(journal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij bijwerken journal {JournalId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Verwijder een journal
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAccountantOrAdmin")]
    public async Task<ActionResult> DeleteJournal(Guid id)
    {
        try
        {
            var result = await _journalService.DeleteJournalAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Journal niet gevonden" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij verwijderen journal {JournalId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }
}
