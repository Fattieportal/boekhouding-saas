using Boekhouding.Application.DTOs.Accounts;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boekhouding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        IAccountService accountService,
        ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Haal alle accounts op met paginering en filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetAccounts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] AccountType? type = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Ongeldige paginering parameters" });
            }

            var (items, totalCount) = await _accountService.GetAccountsAsync(
                page, pageSize, search, type, isActive);

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
            _logger.LogError(ex, "Error bij ophalen accounts");
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Haal een specifieke account op via ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> GetAccount(Guid id)
    {
        try
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound(new { message = "Account niet gevonden" });
            }

            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij ophalen account {AccountId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Haal een account op via code
    /// </summary>
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<AccountDto>> GetAccountByCode(string code)
    {
        try
        {
            var account = await _accountService.GetAccountByCodeAsync(code);
            if (account == null)
            {
                return NotFound(new { message = "Account niet gevonden" });
            }

            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij ophalen account met code {Code}", code);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Maak een nieuwe account aan
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAccountantOrAdmin")]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var account = await _accountService.CreateAccountAsync(dto);
            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij aanmaken account");
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Werk een bestaande account bij
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAccountantOrAdmin")]
    public async Task<ActionResult<AccountDto>> UpdateAccount(Guid id, [FromBody] UpdateAccountDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var account = await _accountService.UpdateAccountAsync(id, dto);
            if (account == null)
            {
                return NotFound(new { message = "Account niet gevonden" });
            }

            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij bijwerken account {AccountId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }

    /// <summary>
    /// Verwijder een account
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAccountantOrAdmin")]
    public async Task<ActionResult> DeleteAccount(Guid id)
    {
        try
        {
            var result = await _accountService.DeleteAccountAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Account niet gevonden" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bij verwijderen account {AccountId}", id);
            return StatusCode(500, new { message = "Er is een fout opgetreden" });
        }
    }
}
