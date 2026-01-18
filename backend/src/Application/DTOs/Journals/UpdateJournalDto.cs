using Boekhouding.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Boekhouding.Application.DTOs.Journals;

public class UpdateJournalDto
{
    [Required(ErrorMessage = "Code is verplicht")]
    [StringLength(20, ErrorMessage = "Code mag maximaal 20 karakters bevatten")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Naam is verplicht")]
    [StringLength(200, ErrorMessage = "Naam mag maximaal 200 karakters bevatten")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type is verplicht")]
    public JournalType Type { get; set; }
}
