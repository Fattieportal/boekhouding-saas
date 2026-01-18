using Boekhouding.Application.DTOs.JournalEntries;

namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Service voor het beheren van journaalposten (boekingen)
/// </summary>
public interface IJournalEntryService
{
    /// <summary>
    /// Maak een nieuwe journaalpost aan (als draft)
    /// </summary>
    Task<JournalEntryDto> CreateEntryAsync(CreateJournalEntryDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update een bestaande journaalpost (alleen als status = Draft)
    /// </summary>
    Task<JournalEntryDto> UpdateEntryAsync(Guid id, UpdateJournalEntryDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Post een journaalpost (status Draft -> Posted)
    /// Validatie: Sum(Debit) moet gelijk zijn aan Sum(Credit)
    /// </summary>
    Task<JournalEntryDto> PostEntryAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Draai een geboekte journaalpost terug door een reversal entry te maken
    /// </summary>
    Task<JournalEntryDto> ReverseEntryAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Haal een journaalpost op op basis van ID
    /// </summary>
    Task<JournalEntryDto?> GetEntryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Haal journaalposten op met optionele filters
    /// </summary>
    Task<List<JournalEntryDto>> GetEntriesAsync(JournalEntryFilterDto filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verwijder een journaalpost (alleen als status = Draft)
    /// </summary>
    Task DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default);
}
