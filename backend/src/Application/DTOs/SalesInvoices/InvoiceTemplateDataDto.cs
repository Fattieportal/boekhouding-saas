namespace Boekhouding.Application.DTOs.SalesInvoices;

public class InvoiceTemplateDataDto
{
    public InvoiceDataDto Invoice { get; set; } = null!;
    public ContactDataDto Contact { get; set; } = null!;
    public List<LineDataDto> Lines { get; set; } = new();
    public BrandingDataDto? Branding { get; set; }
}

public class InvoiceDataDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Subtotal { get; set; } = string.Empty;
    public string VatTotal { get; set; } = string.Empty;
    public string Total { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ContactDataDto
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

public class LineDataDto
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string UnitPrice { get; set; } = string.Empty;
    public string VatRate { get; set; } = string.Empty;
    public string LineSubtotal { get; set; } = string.Empty;
    public string LineVatAmount { get; set; } = string.Empty;
    public string LineTotal { get; set; } = string.Empty;
}

public class BrandingDataDto
{
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#0066cc";
    public string SecondaryColor { get; set; } = "#333333";
    public string FontFamily { get; set; } = "Arial, Helvetica, sans-serif";
    public string? FooterText { get; set; }
}
