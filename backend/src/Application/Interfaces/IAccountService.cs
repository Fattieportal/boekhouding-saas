using Boekhouding.Application.DTOs.Accounts;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.Interfaces;

public interface IAccountService
{
    Task<(IEnumerable<AccountDto> Items, int TotalCount)> GetAccountsAsync(
        int page = 1, 
        int pageSize = 20, 
        string? search = null,
        AccountType? type = null,
        bool? isActive = null);
    
    Task<AccountDto?> GetAccountByIdAsync(Guid id);
    Task<AccountDto?> GetAccountByCodeAsync(string code);
    Task<AccountDto> CreateAccountAsync(CreateAccountDto dto);
    Task<AccountDto?> UpdateAccountAsync(Guid id, UpdateAccountDto dto);
    Task<bool> DeleteAccountAsync(Guid id);
    Task<bool> DeactivateAccountAsync(Guid id);
}
