using Boekhouding.Application.DTOs.JournalEntries;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class JournalEntryService : IJournalEntryService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public JournalEntryService(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<JournalEntryDto> CreateEntryAsync(CreateJournalEntryDto dto, CancellationToken cancellationToken = default)
    {
        // Valideer dat het journal bestaat
        var journalExists = await _context.Journals.AnyAsync(j => j.Id == dto.JournalId, cancellationToken);
        if (!journalExists)
        {
            throw new InvalidOperationException($"Journal met ID {dto.JournalId} niet gevonden.");
        }

        // Valideer dat alle accounts bestaan
        var accountIds = dto.Lines.Select(l => l.AccountId).Distinct().ToList();
        var existingAccountIds = await _context.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var missingAccountIds = accountIds.Except(existingAccountIds).ToList();
        if (missingAccountIds.Any())
        {
            throw new InvalidOperationException($"Accounts niet gevonden: {string.Join(", ", missingAccountIds)}");
        }

        // Valideer dat Debit en Credit niet beide gevuld zijn op dezelfde regel
        foreach (var line in dto.Lines)
        {
            if (line.Debit > 0 && line.Credit > 0)
            {
                throw new InvalidOperationException("Een journaalregel kan niet zowel Debit als Credit bevatten.");
            }
            if (line.Debit < 0 || line.Credit < 0)
            {
                throw new InvalidOperationException("Debit en Credit moeten positief zijn.");
            }
        }

        var entry = new JournalEntry
        {
            JournalId = dto.JournalId,
            EntryDate = DateTime.SpecifyKind(dto.EntryDate, DateTimeKind.Utc),
            Reference = dto.Reference,
            Description = dto.Description,
            Status = JournalEntryStatus.Draft
        };

        foreach (var lineDto in dto.Lines)
        {
            entry.Lines.Add(new JournalLine
            {
                AccountId = lineDto.AccountId,
                Description = lineDto.Description,
                Debit = lineDto.Debit,
                Credit = lineDto.Credit
            });
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetEntryByIdAsync(entry.Id, cancellationToken) 
            ?? throw new InvalidOperationException("Entry kon niet worden opgehaald na aanmaken.");
    }

    public async Task<JournalEntryDto> UpdateEntryAsync(Guid id, UpdateJournalEntryDto dto, CancellationToken cancellationToken = default)
    {
        var entry = await _context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entry == null)
        {
            throw new InvalidOperationException($"Journaalpost met ID {id} niet gevonden.");
        }

        if (entry.Status != JournalEntryStatus.Draft)
        {
            throw new InvalidOperationException($"Kan journaalpost niet updaten: status is {entry.Status}. Alleen Draft entries kunnen worden gewijzigd.");
        }

        // Valideer dat alle accounts bestaan
        var accountIds = dto.Lines.Select(l => l.AccountId).Distinct().ToList();
        var existingAccountIds = await _context.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var missingAccountIds = accountIds.Except(existingAccountIds).ToList();
        if (missingAccountIds.Any())
        {
            throw new InvalidOperationException($"Accounts niet gevonden: {string.Join(", ", missingAccountIds)}");
        }

        // Valideer regels
        foreach (var line in dto.Lines)
        {
            if (line.Debit > 0 && line.Credit > 0)
            {
                throw new InvalidOperationException("Een journaalregel kan niet zowel Debit als Credit bevatten.");
            }
            if (line.Debit < 0 || line.Credit < 0)
            {
                throw new InvalidOperationException("Debit en Credit moeten positief zijn.");
            }
        }

        // Update entry
        entry.EntryDate = DateTime.SpecifyKind(dto.EntryDate, DateTimeKind.Utc);
        entry.Reference = dto.Reference;
        entry.Description = dto.Description;

        // Verwijder oude lines en voeg nieuwe toe
        _context.JournalLines.RemoveRange(entry.Lines);
        entry.Lines.Clear();

        foreach (var lineDto in dto.Lines)
        {
            entry.Lines.Add(new JournalLine
            {
                AccountId = lineDto.AccountId,
                Description = lineDto.Description,
                Debit = lineDto.Debit,
                Credit = lineDto.Credit
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetEntryByIdAsync(entry.Id, cancellationToken) 
            ?? throw new InvalidOperationException("Entry kon niet worden opgehaald na update.");
    }

    public async Task<JournalEntryDto> PostEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entry == null)
        {
            throw new InvalidOperationException($"Journaalpost met ID {id} niet gevonden.");
        }

        if (entry.Status != JournalEntryStatus.Draft)
        {
            throw new InvalidOperationException($"Kan journaalpost niet posten: status is {entry.Status}. Alleen Draft entries kunnen worden geboekt.");
        }

        if (!entry.Lines.Any())
        {
            throw new InvalidOperationException("Kan journaalpost niet posten: geen regels gevonden.");
        }

        // Valideer balans: Sum(Debit) == Sum(Credit)
        var totalDebit = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
        {
            throw new InvalidOperationException(
                $"Kan journaalpost niet posten: balans klopt niet. " +
                $"Totaal Debit: {totalDebit:C}, Totaal Credit: {totalCredit:C}");
        }

        entry.Status = JournalEntryStatus.Posted;
        entry.PostedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetEntryByIdAsync(entry.Id, cancellationToken) 
            ?? throw new InvalidOperationException("Entry kon niet worden opgehaald na posten.");
    }

    public async Task<JournalEntryDto> ReverseEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var originalEntry = await _context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (originalEntry == null)
        {
            throw new InvalidOperationException($"Journaalpost met ID {id} niet gevonden.");
        }

        if (originalEntry.Status != JournalEntryStatus.Posted)
        {
            throw new InvalidOperationException($"Kan alleen geboekte entries terugdraaien. Huidige status: {originalEntry.Status}");
        }

        if (originalEntry.Status == JournalEntryStatus.Reversed)
        {
            throw new InvalidOperationException("Deze entry is al teruggedraaid.");
        }

        // Check of er al een reversal bestaat
        var existingReversal = await _context.JournalEntries
            .AnyAsync(e => e.ReversalOfEntryId == id, cancellationToken);

        if (existingReversal)
        {
            throw new InvalidOperationException("Deze entry heeft al een reversal.");
        }

        // Maak reversal entry met omgekeerde lines
        var reversalEntry = new JournalEntry
        {
            JournalId = originalEntry.JournalId,
            EntryDate = DateTime.UtcNow.Date,
            Reference = $"REVERSAL-{originalEntry.Reference}",
            Description = $"Terugboeking van: {originalEntry.Description}",
            Status = JournalEntryStatus.Posted,
            PostedAt = DateTime.UtcNow,
            ReversalOfEntryId = originalEntry.Id
        };

        foreach (var originalLine in originalEntry.Lines)
        {
            reversalEntry.Lines.Add(new JournalLine
            {
                AccountId = originalLine.AccountId,
                Description = $"Terugboeking: {originalLine.Description}",
                Debit = originalLine.Credit,  // Swap debit en credit
                Credit = originalLine.Debit
            });
        }

        // Update originele entry status
        originalEntry.Status = JournalEntryStatus.Reversed;

        _context.JournalEntries.Add(reversalEntry);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetEntryByIdAsync(reversalEntry.Id, cancellationToken) 
            ?? throw new InvalidOperationException("Reversal entry kon niet worden opgehaald.");
    }

    public async Task<JournalEntryDto?> GetEntryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.JournalEntries
            .Include(e => e.Journal)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .Where(e => e.Id == id)
            .Select(e => new JournalEntryDto
            {
                Id = e.Id,
                TenantId = e.TenantId,
                JournalId = e.JournalId,
                JournalCode = e.Journal.Code,
                JournalName = e.Journal.Name,
                EntryDate = e.EntryDate,
                Reference = e.Reference,
                Description = e.Description,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                PostedAt = e.PostedAt,
                ReversalOfEntryId = e.ReversalOfEntryId,
                Lines = e.Lines.Select(l => new JournalLineDto
                {
                    Id = l.Id,
                    EntryId = l.EntryId,
                    AccountId = l.AccountId,
                    AccountCode = l.Account.Code,
                    AccountName = l.Account.Name,
                    Description = l.Description,
                    Debit = l.Debit,
                    Credit = l.Credit
                }).ToList(),
                TotalDebit = e.Lines.Sum(l => l.Debit),
                TotalCredit = e.Lines.Sum(l => l.Credit)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entry;
    }

    public async Task<List<JournalEntryDto>> GetEntriesAsync(JournalEntryFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.JournalEntries
            .Include(e => e.Journal)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .AsQueryable();

        // Apply filters
        if (filter.JournalId.HasValue)
        {
            query = query.Where(e => e.JournalId == filter.JournalId.Value);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(e => e.EntryDate >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(e => e.EntryDate <= filter.DateTo.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(e => e.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Reference))
        {
            query = query.Where(e => e.Reference.Contains(filter.Reference));
        }

        var entries = await query
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new JournalEntryDto
            {
                Id = e.Id,
                TenantId = e.TenantId,
                JournalId = e.JournalId,
                JournalCode = e.Journal.Code,
                JournalName = e.Journal.Name,
                EntryDate = e.EntryDate,
                Reference = e.Reference,
                Description = e.Description,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                PostedAt = e.PostedAt,
                ReversalOfEntryId = e.ReversalOfEntryId,
                Lines = e.Lines.Select(l => new JournalLineDto
                {
                    Id = l.Id,
                    EntryId = l.EntryId,
                    AccountId = l.AccountId,
                    AccountCode = l.Account.Code,
                    AccountName = l.Account.Name,
                    Description = l.Description,
                    Debit = l.Debit,
                    Credit = l.Credit
                }).ToList(),
                TotalDebit = e.Lines.Sum(l => l.Debit),
                TotalCredit = e.Lines.Sum(l => l.Credit)
            })
            .ToListAsync(cancellationToken);

        return entries;
    }

    public async Task DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entry == null)
        {
            throw new InvalidOperationException($"Journaalpost met ID {id} niet gevonden.");
        }

        if (entry.Status != JournalEntryStatus.Draft)
        {
            throw new InvalidOperationException($"Kan alleen Draft entries verwijderen. Huidige status: {entry.Status}");
        }

        _context.JournalEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
