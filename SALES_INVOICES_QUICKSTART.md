# Sales Invoices - Quick Start Guide

## Prerequisites

- ✅ Backend running (PostgreSQL + .NET 8)
- ✅ Migration applied (`AddSalesInvoicesAndTemplates`)
- ✅ User authenticated with tenant

## Step 1: Install Playwright (Required for PDF)

```powershell
# After building the API project
cd backend/src/Api/bin/Debug/net8.0
pwsh playwright.ps1 install chromium

# Or if playwright is in PATH:
playwright install chromium
```

## Step 2: Run the Test Script

```powershell
cd backend
.\test-sales-invoices.ps1
```

This will:
1. Login with admin credentials
2. Configure branding
3. Create a template
4. Create a contact
5. Create an invoice with 2 lines
6. Generate PDF (if Playwright is installed)
7. List all invoices

## Step 3: Access Frontend

```powershell
cd frontend
npm install  # if first time
npm run dev
```

Visit:
- **Branding**: http://localhost:3000/settings/branding
- **Templates**: http://localhost:3000/settings/templates

## API Endpoints Quick Reference

### Set Branding
```http
PUT /api/tenantbranding
Content-Type: application/json
X-Tenant-Id: {your-tenant-id}
Authorization: Bearer {token}

{
  "logoUrl": "https://example.com/logo.png",
  "primaryColor": "#0066cc",
  "secondaryColor": "#333333",
  "fontFamily": "Arial",
  "footerText": "Company info here"
}
```

### Create Invoice
```http
POST /api/salesinvoices
Content-Type: application/json
X-Tenant-Id: {your-tenant-id}
Authorization: Bearer {token}

{
  "invoiceNumber": "INV-2026-001",
  "issueDate": "2026-01-17",
  "dueDate": "2026-02-17",
  "contactId": "{contact-guid}",
  "currency": "EUR",
  "lines": [
    {
      "description": "Service",
      "quantity": 10,
      "unitPrice": 100.00,
      "vatRate": 21.00
    }
  ]
}
```

### Generate PDF
```http
POST /api/salesinvoices/{invoice-id}/render-pdf
X-Tenant-Id: {your-tenant-id}
Authorization: Bearer {token}

# Returns PDF file for download
```

### List Invoices
```http
GET /api/salesinvoices
X-Tenant-Id: {your-tenant-id}
Authorization: Bearer {token}
```

## Template Syntax Examples

### Variables
```html
{{ Invoice.InvoiceNumber }}
{{ Invoice.IssueDate }}
{{ Invoice.Total }}
{{ Contact.DisplayName }}
{{ Contact.Email }}
{{ Branding.PrimaryColor }}
```

### Loops
```html
{{for line in Lines}}
  <tr>
    <td>{{ line.Description }}</td>
    <td>{{ line.Quantity }}</td>
    <td>{{ line.UnitPrice }}</td>
    <td>{{ line.LineTotal }}</td>
  </tr>
{{end}}
```

### Conditionals
```html
{{if Invoice.Notes}}
  <p>Notes: {{ Invoice.Notes }}</p>
{{end}}

{{if Branding.LogoUrl}}
  <img src="{{ Branding.LogoUrl }}" />
{{end}}
```

## Troubleshooting

### PDF Generation Fails
**Error**: "Playwright not found" or "Browser not installed"

**Solution**:
```powershell
# Install Playwright browsers
playwright install chromium

# Or download manually from:
# https://playwright.dev/docs/browsers
```

### Template Rendering Fails
**Error**: Template parsing errors

**Solution**:
- Check Scriban syntax: https://github.com/scriban/scriban/blob/master/doc/language.md
- Ensure variable names match exactly (case-sensitive)
- Validate HTML is well-formed

### File Storage Issues
**Error**: "Storage path not found"

**Solution**:
- The `./storage` directory is created automatically
- Ensure write permissions on backend directory
- Check disk space

### 404 on API Calls
**Error**: Endpoints not found

**Solution**:
- Ensure backend is running: `dotnet run --project src/Api`
- Check API URL: Default is `https://localhost:7001/api`
- Verify migration was applied: `dotnet ef migrations list`

## Testing Workflow

1. **Set Branding** (optional but recommended)
2. **Create/Use Template** (system has default)
3. **Create Contact** (customer)
4. **Create Invoice** (with lines)
5. **Generate PDF** (test rendering)
6. **Post Invoice** (creates journal entry)

## Default Template Preview

The built-in default template includes:
- Company logo (if provided in branding)
- Invoice number and dates
- Customer information
- Itemized lines table
- Subtotal, VAT, Total
- Footer with company info
- Professional blue color scheme
- Print-ready formatting

## Production Deployment Notes

1. **PDF Generation**
   - Install Playwright on server
   - Or switch to QuestPDF/DinkToPdf for no dependencies
   
2. **File Storage**
   - Current: Local filesystem (`./storage`)
   - Production: Implement Azure Blob or AWS S3
   - Just swap `IFileStorage` implementation

3. **Performance**
   - Consider background job queue for PDF generation
   - Cache rendered templates
   - Use CDN for logo files

4. **Security**
   - Validate template syntax before saving
   - Scan uploaded files
   - Implement rate limiting on PDF generation

## Support Resources

- **Full Documentation**: `SALES_INVOICES_README.md`
- **Implementation Details**: `SALES_INVOICES_IMPLEMENTATION_SUMMARY.md`
- **Test Script**: `backend/test-sales-invoices.ps1`
- **Scriban Docs**: https://github.com/scriban/scriban
- **Playwright Docs**: https://playwright.dev/dotnet/

## Quick Commands

```powershell
# Build
cd backend
dotnet build

# Run migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Api

# Run API
dotnet run --project src/Api

# Run frontend
cd frontend
npm run dev

# Test API
.\test-sales-invoices.ps1

# Install Playwright
playwright install chromium
```

Happy invoicing! 🎉
