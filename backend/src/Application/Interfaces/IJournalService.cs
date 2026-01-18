using Boekhouding.Application.DTOs.Journals;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.Interfaces;

public interface IJournalService
{
    Task<(IEnumerable<JournalDto> Items, int TotalCount)> GetJournalsAsync(
        int page = 1, 
        int pageSize = 20, 
        string? search = null,
        JournalType? type = null);
    
    Task<JournalDto?> GetJournalByIdAsync(Guid id);
    Task<JournalDto?> GetJournalByCodeAsync(string code);
    Task<JournalDto> CreateJournalAsync(CreateJournalDto dto);
    Task<JournalDto?> UpdateJournalAsync(Guid id, UpdateJournalDto dto);
    Task<bool> DeleteJournalAsync(Guid id);
}
