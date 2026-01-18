using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.DTOs.Contacts;

public class ContactDto
{
    public Guid Id { get; set; }
    public ContactType Type { get; set; }
    public string TypeName => Type.ToString();
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string Country { get; set; } = "NL";
    public string? VatNumber { get; set; }
    public string? KvK { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
