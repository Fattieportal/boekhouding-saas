using Boekhouding.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Boekhouding.Application.DTOs.Contacts;

public class UpdateContactDto
{
    [Required(ErrorMessage = "Type is verplicht")]
    public ContactType Type { get; set; }

    [Required(ErrorMessage = "DisplayName is verplicht")]
    [StringLength(200, ErrorMessage = "DisplayName mag maximaal 200 karakters bevatten")]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ongeldig email formaat")]
    [StringLength(200, ErrorMessage = "Email mag maximaal 200 karakters bevatten")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "Phone mag maximaal 50 karakters bevatten")]
    public string? Phone { get; set; }

    [StringLength(200, ErrorMessage = "AddressLine1 mag maximaal 200 karakters bevatten")]
    public string? AddressLine1 { get; set; }

    [StringLength(200, ErrorMessage = "AddressLine2 mag maximaal 200 karakters bevatten")]
    public string? AddressLine2 { get; set; }

    [StringLength(20, ErrorMessage = "PostalCode mag maximaal 20 karakters bevatten")]
    public string? PostalCode { get; set; }

    [StringLength(100, ErrorMessage = "City mag maximaal 100 karakters bevatten")]
    public string? City { get; set; }

    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country moet precies 2 karakters zijn")]
    public string Country { get; set; } = "NL";

    [StringLength(50, ErrorMessage = "VatNumber mag maximaal 50 karakters bevatten")]
    public string? VatNumber { get; set; }

    [StringLength(50, ErrorMessage = "KvK mag maximaal 50 karakters bevatten")]
    public string? KvK { get; set; }

    public bool IsActive { get; set; } = true;
}
