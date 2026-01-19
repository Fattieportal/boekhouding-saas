using Boekhouding.Application.DTOs.Contacts;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Boekhouding.Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public ContactService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    public async Task<(IEnumerable<ContactDto> Items, int TotalCount)> GetContactsAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        ContactType? type = null,
        bool? isActive = null)
    {
        var query = _context.Contacts.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => 
                c.DisplayName.Contains(search) || 
                (c.Email != null && c.Email.Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)) ||
                (c.VatNumber != null && c.VatNumber.Contains(search)) ||
                (c.KvK != null && c.KvK.Contains(search)));
        }

        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply paging and ordering
        var items = await query
            .OrderBy(c => c.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContactDto
            {
                Id = c.Id,
                Type = c.Type,
                DisplayName = c.DisplayName,
                Email = c.Email,
                Phone = c.Phone,
                AddressLine1 = c.AddressLine1,
                AddressLine2 = c.AddressLine2,
                PostalCode = c.PostalCode,
                City = c.City,
                Country = c.Country,
                VatNumber = c.VatNumber,
                KvK = c.KvK,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ContactDto?> GetContactByIdAsync(Guid id)
    {
        var contact = await _context.Contacts
            .Where(c => c.Id == id)
            .Select(c => new ContactDto
            {
                Id = c.Id,
                Type = c.Type,
                DisplayName = c.DisplayName,
                Email = c.Email,
                Phone = c.Phone,
                AddressLine1 = c.AddressLine1,
                AddressLine2 = c.AddressLine2,
                PostalCode = c.PostalCode,
                City = c.City,
                Country = c.Country,
                VatNumber = c.VatNumber,
                KvK = c.KvK,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return contact;
    }

    public async Task<ContactDto> CreateContactAsync(CreateContactDto dto)
    {
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            Phone = dto.Phone,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            PostalCode = dto.PostalCode,
            City = dto.City,
            Country = dto.Country,
            VatNumber = dto.VatNumber,
            KvK = dto.KvK,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Audit log
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUserService.GetUserId();
        
        if (tenantId.HasValue && userId.HasValue)
        {
            await _auditLogService.LogAsync(
                tenantId.Value,
                userId.Value,
                "Contact.Create",
                "Contact",
                contact.Id,
                new { contact.DisplayName, contact.Type, contact.Email });
        }

        return new ContactDto
        {
            Id = contact.Id,
            Type = contact.Type,
            DisplayName = contact.DisplayName,
            Email = contact.Email,
            Phone = contact.Phone,
            AddressLine1 = contact.AddressLine1,
            AddressLine2 = contact.AddressLine2,
            PostalCode = contact.PostalCode,
            City = contact.City,
            Country = contact.Country,
            VatNumber = contact.VatNumber,
            KvK = contact.KvK,
            IsActive = contact.IsActive,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
    }

    public async Task<ContactDto?> UpdateContactAsync(Guid id, UpdateContactDto dto)
    {
        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);
        if (contact == null)
        {
            return null;
        }

        contact.Type = dto.Type;
        contact.DisplayName = dto.DisplayName;
        contact.Email = dto.Email;
        contact.Phone = dto.Phone;
        contact.AddressLine1 = dto.AddressLine1;
        contact.AddressLine2 = dto.AddressLine2;
        contact.PostalCode = dto.PostalCode;
        contact.City = dto.City;
        contact.Country = dto.Country;
        contact.VatNumber = dto.VatNumber;
        contact.KvK = dto.KvK;
        contact.IsActive = dto.IsActive;
        contact.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUserService.GetUserId();
        
        if (tenantId.HasValue && userId.HasValue)
        {
            await _auditLogService.LogAsync(
                tenantId.Value,
                userId.Value,
                "UPDATE",
                "Contact",
                contact.Id,
                new { 
                    contact.DisplayName, 
                    contact.Type, 
                    contact.Email,
                    contact.VatNumber,
                    UpdatedFields = dto
                });
        }

        return new ContactDto
        {
            Id = contact.Id,
            Type = contact.Type,
            DisplayName = contact.DisplayName,
            Email = contact.Email,
            Phone = contact.Phone,
            AddressLine1 = contact.AddressLine1,
            AddressLine2 = contact.AddressLine2,
            PostalCode = contact.PostalCode,
            City = contact.City,
            Country = contact.Country,
            VatNumber = contact.VatNumber,
            KvK = contact.KvK,
            IsActive = contact.IsActive,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
    }

    public async Task<bool> DeleteContactAsync(Guid id)
    {
        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);
        if (contact == null)
        {
            return false;
        }

        // Audit log BEFORE deletion
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUserService.GetUserId();
        
        if (tenantId.HasValue && userId.HasValue)
        {
            await _auditLogService.LogAsync(
                tenantId.Value,
                userId.Value,
                "DELETE",
                "Contact",
                contact.Id,
                new { 
                    contact.DisplayName, 
                    contact.Type, 
                    contact.Email 
                });
        }

        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync();

        return true;
    }
}
