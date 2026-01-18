using Boekhouding.Application.DTOs.Journals;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class JournalService : IJournalService
{
    private readonly ApplicationDbContext _context;

    public JournalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<JournalDto> Items, int TotalCount)> GetJournalsAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        JournalType? type = null)
    {
        var query = _context.Journals.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(j => 
                j.Code.Contains(search) || 
                j.Name.Contains(search));
        }

        if (type.HasValue)
        {
            query = query.Where(j => j.Type == type.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply paging and ordering
        var items = await query
            .OrderBy(j => j.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JournalDto
            {
                Id = j.Id,
                Code = j.Code,
                Name = j.Name,
                Type = j.Type,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<JournalDto?> GetJournalByIdAsync(Guid id)
    {
        var journal = await _context.Journals
            .Where(j => j.Id == id)
            .Select(j => new JournalDto
            {
                Id = j.Id,
                Code = j.Code,
                Name = j.Name,
                Type = j.Type,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return journal;
    }

    public async Task<JournalDto?> GetJournalByCodeAsync(string code)
    {
        var journal = await _context.Journals
            .Where(j => j.Code == code)
            .Select(j => new JournalDto
            {
                Id = j.Id,
                Code = j.Code,
                Name = j.Name,
                Type = j.Type,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return journal;
    }

    public async Task<JournalDto> CreateJournalAsync(CreateJournalDto dto)
    {
        // Check if code already exists for this tenant
        var exists = await _context.Journals.AnyAsync(j => j.Code == dto.Code);
        if (exists)
        {
            throw new InvalidOperationException($"Journal met code '{dto.Code}' bestaat al voor deze tenant.");
        }

        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            CreatedAt = DateTime.UtcNow
        };

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        return new JournalDto
        {
            Id = journal.Id,
            Code = journal.Code,
            Name = journal.Name,
            Type = journal.Type,
            CreatedAt = journal.CreatedAt,
            UpdatedAt = journal.UpdatedAt
        };
    }

    public async Task<JournalDto?> UpdateJournalAsync(Guid id, UpdateJournalDto dto)
    {
        var journal = await _context.Journals.FirstOrDefaultAsync(j => j.Id == id);
        if (journal == null)
        {
            return null;
        }

        // Check if new code already exists (excluding current journal)
        if (journal.Code != dto.Code)
        {
            var codeExists = await _context.Journals.AnyAsync(j => j.Code == dto.Code && j.Id != id);
            if (codeExists)
            {
                throw new InvalidOperationException($"Journal met code '{dto.Code}' bestaat al voor deze tenant.");
            }
        }

        journal.Code = dto.Code;
        journal.Name = dto.Name;
        journal.Type = dto.Type;
        journal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new JournalDto
        {
            Id = journal.Id,
            Code = journal.Code,
            Name = journal.Name,
            Type = journal.Type,
            CreatedAt = journal.CreatedAt,
            UpdatedAt = journal.UpdatedAt
        };
    }

    public async Task<bool> DeleteJournalAsync(Guid id)
    {
        var journal = await _context.Journals.FirstOrDefaultAsync(j => j.Id == id);
        if (journal == null)
        {
            return false;
        }

        _context.Journals.Remove(journal);
        await _context.SaveChangesAsync();

        return true;
    }
}
