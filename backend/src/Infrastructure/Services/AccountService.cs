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

    public AccountService(ApplicationDbContext context)
    {
        _context = context;
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
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return null;
        }

        // Check if new code already exists (excluding current account)
        if (account.Code != dto.Code)
        {
            var codeExists = await _context.Accounts.AnyAsync(a => a.Code == dto.Code && a.Id != id);
            if (codeExists)
            {
                throw new InvalidOperationException($"Account met code '{dto.Code}' bestaat al voor deze tenant.");
            }
        }

        account.Code = dto.Code;
        account.Name = dto.Name;
        account.Type = dto.Type;
        account.IsActive = dto.IsActive;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

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
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return false;
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        return true;
    }
}
