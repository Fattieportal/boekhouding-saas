namespace Boekhouding.Application.DTOs.InvoiceTemplates;

public class InvoiceTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string HtmlTemplate { get; set; } = string.Empty;
    public string CssTemplate { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateInvoiceTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string HtmlTemplate { get; set; } = string.Empty;
    public string CssTemplate { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
}

public class UpdateInvoiceTemplateDto
{
    public string? Name { get; set; }
    public bool? IsDefault { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? CssTemplate { get; set; }
    public string? SettingsJson { get; set; }
}
