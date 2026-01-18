namespace Boekhouding.Application.DTOs.Tenants;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? KvK { get; set; }
    public string? VatNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class CreateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string? KvK { get; set; }
    public string? VatNumber { get; set; }
}

public class TenantListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
