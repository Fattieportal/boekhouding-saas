namespace Boekhouding.Domain.Common;

/// <summary>
/// Basis entiteit klasse voor alle domain entiteiten
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
