using Boekhouding.Domain.Enums;

namespace Boekhouding.Application.DTOs.Journals;

public class JournalDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JournalType Type { get; set; }
    public string TypeName => Type.ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
