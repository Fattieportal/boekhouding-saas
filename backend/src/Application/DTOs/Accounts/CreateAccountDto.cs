using Boekhouding.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Boekhouding.Application.DTOs.Accounts;

public class CreateAccountDto
{
    [Required(ErrorMessage = "Code is verplicht")]
    [StringLength(20, ErrorMessage = "Code mag maximaal 20 karakters bevatten")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Naam is verplicht")]
    [StringLength(200, ErrorMessage = "Naam mag maximaal 200 karakters bevatten")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type is verplicht")]
    public AccountType Type { get; set; }

    public bool IsActive { get; set; } = true;
}
