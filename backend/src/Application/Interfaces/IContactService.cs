using Boekhouding.Application.DTOs.Contacts;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.Interfaces;

public interface IContactService
{
    Task<(IEnumerable<ContactDto> Items, int TotalCount)> GetContactsAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        ContactType? type = null,
        bool? isActive = null);

    Task<ContactDto?> GetContactByIdAsync(Guid id);
    Task<ContactDto> CreateContactAsync(CreateContactDto dto);
    Task<ContactDto?> UpdateContactAsync(Guid id, UpdateContactDto dto);
    Task<bool> DeleteContactAsync(Guid id);
}
