using Boekhouding.Application.DTOs.Contacts;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(
        IContactService contactService,
        ILogger<ContactsController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    /// <summary>
    /// Haal alle contacten op met paginering en filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetContacts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        [FromQuery] ContactType? type = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Ongeldige paginering parameters" });
            }

            var (items, totalCount) = await _contactService.GetContactsAsync(
                page, pageSize, q, type, isActive);

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
            _logger.LogError(ex, "Error bij ophalen contacten");
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Haal een specifiek contact op via ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ContactDto>> GetContact(Guid id)
    {
        try
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            if (contact == null)
            {
                return NotFound(new { message = "Contact niet gevonden" });
            }

            return Ok(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij ophalen contact {ContactId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Maak een nieuw contact aan
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ContactDto>> CreateContact([FromBody] CreateContactDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var contact = await _contactService.CreateContactAsync(dto);
            return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij aanmaken contact");
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Update een bestaand contact
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ContactDto>> UpdateContact(Guid id, [FromBody] UpdateContactDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var contact = await _contactService.UpdateContactAsync(id, dto);
            if (contact == null)
            {
                return NotFound(new { message = "Contact niet gevonden" });
            }

            return Ok(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij updaten contact {ContactId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Verwijder een contact
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteContact(Guid id)
    {
        try
        {
            var result = await _contactService.DeleteContactAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Contact niet gevonden" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij verwijderen contact {ContactId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }
}
