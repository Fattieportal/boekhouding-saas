# Sales Invoices & PDF Implementation

## Overview

Professional sales invoice system with templateable PDF generation for the boekhouding SaaS application.

## Features

✅ **Invoice Templates**
- Multiple templates per tenant
- One default template
- HTML/CSS based templates with Scriban syntax (Liquid-like)
- Template preview and editing

✅ **Tenant Branding**
- Logo customization
- Primary and secondary color theming
- Custom font family
- Footer text for company information

✅ **Sales Invoices**
- Full CRUD operations
- Status workflow: Draft → Sent → Posted → Paid
- Multiple invoice lines with VAT calculations
- Automatic total calculations
- Contact/customer linking

✅ **PDF Generation**
- Template-based rendering (Scriban)
- PDF generation via Playwright
- Local file storage with metadata
- Download and preview capabilities

✅ **Accounting Integration**
- Post invoices to create journal entries
- Automatic debit/credit lines
- VAT handling

## Architecture

### Domain Entities

1. **InvoiceTemplate**
   - Template name and settings
   - HTML and CSS content
   - Default flag per tenant

2. **TenantBranding**
   - Logo URL
   - Color scheme (primary/secondary)
   - Font family
   - Footer text

3. **SalesInvoice**
   - Invoice header (number, dates, customer)
   - Status tracking
   - Totals (subtotal, VAT, total)
   - PDF file reference
   - Journal entry reference

4. **SalesInvoiceLine**
   - Line items
   - Quantity, price, VAT rate
   - Calculated totals

5. **StoredFile**
   - File metadata (name, size, type)
   - Storage path
   - Category tagging

### Infrastructure Services

#### Template Rendering
```csharp
ITemplateRenderer (ScribanTemplateRenderer)
```
- Uses Scriban library for Liquid-like template syntax
- Supports variables: `{{ Invoice.InvoiceNumber }}`, `{{ Contact.DisplayName }}`
- Control flow: `{{if}}`, `{{for}}`, etc.

#### PDF Generation
```csharp
IPdfRenderer (PlaywrightPdfRenderer)
```
- Uses Microsoft Playwright for browser automation
- Generates PDFs from HTML/CSS
- Configurable page settings (A4, margins)
- **Note**: Requires Playwright browsers to be installed

#### File Storage
```csharp
IFileStorage (LocalFileStorage)
```
- Stores files in `./storage` directory
- Organized by tenant and category
- Tracks metadata in database
- **Production**: Can be swapped for cloud storage (Azure Blob, S3, etc.)

## API Endpoints

### Invoice Templates

```http
GET    /api/invoicetemplates           # List all templates
GET    /api/invoicetemplates/{id}      # Get template by ID
GET    /api/invoicetemplates/default   # Get default template
POST   /api/invoicetemplates           # Create template
PUT    /api/invoicetemplates/{id}      # Update template
DELETE /api/invoicetemplates/{id}      # Delete template
POST   /api/invoicetemplates/{id}/set-default  # Set as default
```

### Tenant Branding

```http
GET  /api/tenantbranding     # Get branding settings
PUT  /api/tenantbranding     # Create or update branding
```

### Sales Invoices

```http
GET    /api/salesinvoices              # List all invoices
GET    /api/salesinvoices/{id}         # Get invoice by ID
POST   /api/salesinvoices              # Create invoice
PUT    /api/salesinvoices/{id}         # Update invoice (draft only)
DELETE /api/salesinvoices/{id}         # Delete invoice (draft only)
POST   /api/salesinvoices/{id}/render-pdf    # Generate and download PDF
GET    /api/salesinvoices/{id}/download-pdf  # Download existing PDF
POST   /api/salesinvoices/{id}/post          # Post to accounting
```

## Database Schema

### Migration: `AddSalesInvoicesAndTemplates`

Tables created:
- `InvoiceTemplates`
- `TenantBrandings`
- `SalesInvoices`
- `SalesInvoiceLines`
- `StoredFiles`

All tables include:
- Multi-tenant support (TenantId with query filters)
- Audit fields (CreatedAt, UpdatedAt)
- Proper foreign key relationships

## Installation & Setup

### 1. Install NuGet Packages

Already added to `Infrastructure.csproj`:
```xml
<PackageReference Include="Scriban" Version="5.10.0" />
<PackageReference Include="Microsoft.Playwright" Version="1.48.0" />
```

### 2. Install Playwright Browsers

```powershell
cd backend/src/Api/bin/Debug/net8.0
pwsh playwright.ps1 install chromium
```

Or build the project first, then:
```powershell
playwright install chromium
```

### 3. Run Migration

```powershell
cd backend
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

### 4. Create Storage Directory

The application automatically creates `./storage` in the backend directory, organized as:
```
storage/
  {tenant-id}/
    Invoices/
      {file-id}.pdf
    Logos/
      {file-id}.png
```

## Frontend Pages

### Branding Settings
**URL**: `/settings/branding`

Features:
- Logo URL input
- Color pickers for primary/secondary colors
- Font family selector
- Footer text editor
- Live preview

### Template Manager
**URL**: `/settings/templates`

Features:
- Template list with default indicator
- HTML/CSS code editors
- Scriban syntax support
- Set default template
- Create/edit/delete templates

## Default Template

A professional default template is built-in and includes:
- Company logo display
- Invoice header with number and dates
- Customer information section
- Line items table
- Subtotal, VAT, and total calculations
- Footer with company information
- Responsive design
- Print-ready formatting

Template syntax example:
```html
<h1>INVOICE {{ Invoice.InvoiceNumber }}</h1>
<p>Date: {{ Invoice.IssueDate }}</p>
<p>Customer: {{ Contact.DisplayName }}</p>

{{for line in Lines}}
  <tr>
    <td>{{ line.Description }}</td>
    <td>{{ line.Quantity }}</td>
    <td>{{ Invoice.Currency }} {{ line.UnitPrice }}</td>
  </tr>
{{end}}
```

## Usage Example

### 1. Set Up Branding
```http
PUT /api/tenantbranding
{
  "logoUrl": "https://example.com/logo.png",
  "primaryColor": "#0066cc",
  "secondaryColor": "#333333",
  "fontFamily": "Arial, Helvetica, sans-serif",
  "footerText": "Company Inc.\nVAT: NL123456789B01\nKvK: 12345678"
}
```

### 2. Create Invoice
```http
POST /api/salesinvoices
{
  "invoiceNumber": "INV-2026-001",
  "issueDate": "2026-01-17",
  "dueDate": "2026-02-17",
  "contactId": "{customer-guid}",
  "currency": "EUR",
  "lines": [
    {
      "description": "Consulting services",
      "quantity": 10,
      "unitPrice": 100.00,
      "vatRate": 21.00
    }
  ]
}
```

### 3. Generate PDF
```http
POST /api/salesinvoices/{invoice-id}/render-pdf
```
Returns: PDF file for download

### 4. Post to Accounting
```http
POST /api/salesinvoices/{invoice-id}/post
```
Creates journal entry with debit/credit lines

## Invoice Status Workflow

1. **Draft** → Initial state, can be edited/deleted
2. **Sent** → Sent to customer, can still be posted
3. **Posted** → Booked in accounting, creates journal entry
4. **Paid** → Payment received

**Rules**:
- Only Draft invoices can be updated or deleted
- Draft and Sent invoices can be posted
- Posted invoices create journal entries

## Template Variables Reference

### Invoice Object
- `Invoice.InvoiceNumber`
- `Invoice.IssueDate`
- `Invoice.DueDate`
- `Invoice.Currency`
- `Invoice.Subtotal`
- `Invoice.VatTotal`
- `Invoice.Total`
- `Invoice.Notes`

### Contact Object
- `Contact.DisplayName`
- `Contact.Email`
- `Contact.Phone`
- `Contact.AddressLine1`
- `Contact.AddressLine2`
- `Contact.PostalCode`
- `Contact.City`
- `Contact.Country`

### Lines Array
```scriban
{{for line in Lines}}
  {{ line.LineNumber }}
  {{ line.Description }}
  {{ line.Quantity }}
  {{ line.UnitPrice }}
  {{ line.VatRate }}
  {{ line.LineSubtotal }}
  {{ line.LineVatAmount }}
  {{ line.LineTotal }}
{{end}}
```

### Branding Object
- `Branding.LogoUrl`
- `Branding.PrimaryColor`
- `Branding.SecondaryColor`
- `Branding.FontFamily`
- `Branding.FooterText`

## Production Considerations

### PDF Generation
- **Current**: Playwright (requires Chrome/Chromium installation)
- **Alternatives**: 
  - QuestPDF (pure .NET, no dependencies)
  - DinkToPdf (wkhtmltopdf wrapper)
  - IronPdf (commercial)

### File Storage
- **Current**: Local filesystem (`./storage`)
- **Production**: 
  - Implement cloud storage (Azure Blob Storage, AWS S3)
  - Update `IFileStorage` implementation
  - No code changes needed in services (interface-based)

### Performance
- Template rendering is cached by Scriban
- PDF generation can be async/background job for large documents
- Consider queue-based processing for high volume

### Security
- Templates are tenant-isolated
- File storage is tenant-segregated
- PDF access requires authentication and tenant context
- Validate template syntax before saving

## Testing

Create a test script `test-sales-invoices.ps1`:

```powershell
# Test workflow
$token = "your-jwt-token"
$tenantId = "your-tenant-id"
$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
    "Content-Type" = "application/json"
}

# 1. Set branding
# 2. Create invoice
# 3. Generate PDF
# 4. Post invoice
```

## Future Enhancements

- [ ] Email invoice PDFs to customers
- [ ] Invoice numbering sequences per tenant
- [ ] Recurring invoices
- [ ] Invoice templates marketplace
- [ ] Multi-language support
- [ ] Credit notes
- [ ] Payment tracking integration
- [ ] Advanced template editor with WYSIWYG
- [ ] Batch PDF generation
- [ ] E-invoicing standards (UBL, Peppol)

## Support

For issues or questions:
1. Check migration was applied: `dotnet ef migrations list`
2. Verify Playwright installation: `playwright --version`
3. Check storage directory permissions
4. Review application logs for rendering errors

## License

Part of the Boekhouding SaaS application.
