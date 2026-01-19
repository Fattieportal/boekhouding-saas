using Boekhouding.Application.DTOs.Accounts;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLog;
    private readonly IUserContext _userContext;

    public AccountService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        IAuditLogService auditLog,
        IUserContext userContext)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditLog = auditLog;
        _userContext = userContext;
    }

    public async Task<(IEnumerable<AccountDto> Items, int TotalCount)> GetAccountsAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        AccountType? type = null,
        bool? isActive = null)
    {
        var query = _context.Accounts.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => 
                a.Code.Contains(search) || 
                a.Name.Contains(search));
        }

        if (type.HasValue)
        {
            query = query.Where(a => a.Type == type.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply paging and ordering
        var items = await query
            .OrderBy(a => a.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Type = a.Type,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<AccountDto?> GetAccountByIdAsync(Guid id)
    {
        var account = await _context.Accounts
            .Where(a => a.Id == id)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Type = a.Type,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return account;
    }

    public async Task<AccountDto?> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Accounts
            .Where(a => a.Code == code)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Type = a.Type,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return account;
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto dto)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        
        // Check if code already exists for this tenant
        var exists = await _context.Accounts.AnyAsync(a => a.Code == dto.Code);
        if (exists)
        {
            throw new InvalidOperationException($"Account met code '{dto.Code}' bestaat al voor deze tenant.");
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Audit log for account creation
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");
        await _auditLog.LogAsync(
            tenantId,
            userId,
            "CREATE_ACCOUNT",
            "Account",
            account.Id,
            new {
                Code = account.Code,
                Name = account.Name,
                Type = account.Type.ToString(),
                IsActive = account.IsActive,
                Message = $"Chart of Accounts: Created account {account.Code} - {account.Name}"
            });

        return new AccountDto
        {
            Id = account.Id,
            Code = account.Code,
            Name = account.Name,
            Type = account.Type,
            IsActive = account.IsActive,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    public async Task<AccountDto?> UpdateAccountAsync(Guid id, UpdateAccountDto dto)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return null;
        }

        // Track changes for audit
        var changes = new Dictionary<string, object>();
        var oldCode = account.Code;
        var oldName = account.Name;
        var oldType = account.Type;
        var oldIsActive = account.IsActive;

        // Check if new code already exists (excluding current account)
        if (account.Code != dto.Code)
        {
            var codeExists = await _context.Accounts.AnyAsync(a => a.Code == dto.Code && a.Id != id);
            if (codeExists)
            {
                throw new InvalidOperationException($"Account met code '{dto.Code}' bestaat al voor deze tenant.");
            }
            changes["Code"] = new { Old = oldCode, New = dto.Code };
        }

        if (account.Name != dto.Name)
            changes["Name"] = new { Old = oldName, New = dto.Name };
        
        if (account.Type != dto.Type)
            changes["Type"] = new { Old = oldType.ToString(), New = dto.Type.ToString() };
        
        if (account.IsActive != dto.IsActive)
            changes["IsActive"] = new { Old = oldIsActive, New = dto.IsActive };

        account.Code = dto.Code;
        account.Name = dto.Name;
        account.Type = dto.Type;
        account.IsActive = dto.IsActive;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log for account update
        if (changes.Any())
        {
            var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");
            await _auditLog.LogAsync(
                tenantId,
                userId,
                "UPDATE_ACCOUNT",
                "Account",
                account.Id,
                new {
                    Code = account.Code,
                    Name = account.Name,
                    Type = account.Type.ToString(),
                    UpdatedFields = changes,
                    Message = $"Chart of Accounts: Updated account {account.Code} - {account.Name}"
                });
        }

        return new AccountDto
        {
            Id = account.Id,
            Code = account.Code,
            Name = account.Name,
            Type = account.Type,
            IsActive = account.IsActive,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    public async Task<bool> DeleteAccountAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return false;
        }

        // Store info before deletion for audit
        var accountCode = account.Code;
        var accountName = account.Name;
        var accountType = account.Type;

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        // Audit log for account deletion
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");
        await _auditLog.LogAsync(
            tenantId,
            userId,
            "DELETE_ACCOUNT",
            "Account",
            id,
            new {
                Code = accountCode,
                Name = accountName,
                Type = accountType.ToString(),
                Message = $"Chart of Accounts: Deleted account {accountCode} - {accountName}"
            });

        return true;
    }

    public async Task<bool> DeactivateAccountAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is not set");
        
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return false;
        }

        if (!account.IsActive)
        {
            return true; // Already deactivated
        }

        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log for account deactivation
        var userId = _userContext.UserId ?? throw new InvalidOperationException("User context is not set");
        await _auditLog.LogAsync(
            tenantId,
            userId,
            "DEACTIVATE_ACCOUNT",
            "Account",
            account.Id,
            new {
                Code = account.Code,
                Name = account.Name,
                Type = account.Type.ToString(),
                Message = $"Chart of Accounts: Deactivated account {account.Code} - {account.Name}"
            });

        return true;
    }
}
