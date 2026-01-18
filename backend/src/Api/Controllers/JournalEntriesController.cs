using Boekhouding.Application.DTOs.JournalEntries;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/journal-entries")]
[Authorize]
public class JournalEntriesController : ControllerBase
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly ILogger<JournalEntriesController> _logger;

    public JournalEntriesController(
        IJournalEntryService journalEntryService,
        ILogger<JournalEntriesController> logger)
    {
        _journalEntryService = journalEntryService;
        _logger = logger;
    }

    /// <summary>
    /// Haal journaalposten op met optionele filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<JournalEntryDto>>> GetEntries(
        [FromQuery] Guid? journalId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] JournalEntryStatus? status = null,
        [FromQuery] string? reference = null)
    {
        try
        {
            var filter = new JournalEntryFilterDto
            {
                JournalId = journalId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Status = status,
                Reference = reference
            };

            var entries = await _journalEntryService.GetEntriesAsync(filter);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij ophalen journal entries");
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Haal een specifieke journaalpost op via ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<JournalEntryDto>> GetEntry(Guid id)
    {
        try
        {
            var entry = await _journalEntryService.GetEntryByIdAsync(id);
            if (entry == null)
            {
                return NotFound(new { message = "Journaalpost niet gevonden" });
            }

            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij ophalen journal entry {EntryId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Maak een nieuwe journaalpost aan (draft)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<JournalEntryDto>> CreateEntry([FromBody] CreateJournalEntryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entry = await _journalEntryService.CreateEntryAsync(dto);
            return CreatedAtAction(nameof(GetEntry), new { id = entry.Id }, entry);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validatiefout bij aanmaken journal entry");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij aanmaken journal entry");
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Update een bestaande journaalpost (alleen draft)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<JournalEntryDto>> UpdateEntry(Guid id, [FromBody] UpdateJournalEntryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entry = await _journalEntryService.UpdateEntryAsync(id, dto);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validatiefout bij updaten journal entry {EntryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij updaten journal entry {EntryId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Post een journaalpost (draft -> posted)
    /// </summary>
    [HttpPost("{id}/post")]
    public async Task<ActionResult<JournalEntryDto>> PostEntry(Guid id)
    {
        try
        {
            var entry = await _journalEntryService.PostEntryAsync(id);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validatiefout bij posten journal entry {EntryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij posten journal entry {EntryId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Draai een geboekte journaalpost terug via reversal
    /// </summary>
    [HttpPost("{id}/reverse")]
    public async Task<ActionResult<JournalEntryDto>> ReverseEntry(Guid id)
    {
        try
        {
            var reversalEntry = await _journalEntryService.ReverseEntryAsync(id);
            return Ok(reversalEntry);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validatiefout bij terugdraaien journal entry {EntryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij terugdraaien journal entry {EntryId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Verwijder een journaalpost (alleen draft)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEntry(Guid id)
    {
        try
        {
            await _journalEntryService.DeleteEntryAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validatiefout bij verwijderen journal entry {EntryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij verwijderen journal entry {EntryId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }
}
