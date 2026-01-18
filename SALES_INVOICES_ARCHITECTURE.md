# Sales Invoices Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend (Next.js)                       │
├─────────────────────────────────────────────────────────────────┤
│  /settings/branding       │  /settings/templates                │
│  - Logo configuration     │  - HTML/CSS editor                  │
│  - Color picker           │  - Template management              │
│  - Live preview           │  - Set default                      │
└──────────────────────┬──────────────────────────────────────────┘
                       │ REST API (HTTPS + JWT)
┌──────────────────────▼──────────────────────────────────────────┐
│                      API Layer (Controllers)                     │
├─────────────────────────────────────────────────────────────────┤
│  InvoiceTemplatesController  │  TenantBrandingController        │
│  SalesInvoicesController     │  (+ existing controllers)        │
└──────────────────────┬──────────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────────┐
│                   Application Layer (Services)                   │
├─────────────────────────────────────────────────────────────────┤
│  IInvoiceTemplateService  │  ISalesInvoiceService               │
│  ITenantBrandingService   │  ITemplateRenderer                  │
│  IPdfRenderer             │  IFileStorage                       │
└──────────────────────┬──────────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────────┐
│                Infrastructure Layer (Implementations)            │
├─────────────────────────────────────────────────────────────────┤
│  InvoiceTemplateService        │  SalesInvoiceService           │
│  TenantBrandingService         │  ScribanTemplateRenderer       │
│  PlaywrightPdfRenderer         │  LocalFileStorage              │
└──────────────┬────────────────────────────┬─────────────────────┘
               │                            │
      ┌────────▼─────────┐         ┌───────▼──────────┐
      │   PostgreSQL     │         │  File System     │
      │   (EF Core)      │         │  ./storage/      │
      │                  │         │  {tenant-id}/    │
      │  - Templates     │         │    Invoices/     │
      │  - Branding      │         │    Logos/        │
      │  - Invoices      │         │                  │
      │  - Lines         │         │  StoredFile      │
      │  - Files         │         │  metadata in DB  │
      └──────────────────┘         └──────────────────┘
```

## Data Flow: Invoice PDF Generation

```
1. User Request
   POST /api/salesinvoices/{id}/render-pdf
   │
   ▼
2. SalesInvoicesController
   - Validates request
   - Calls ISalesInvoiceService.RenderInvoicePdfAsync()
   │
   ▼
3. SalesInvoiceService
   ├─► Get Invoice + Lines from DB
   ├─► Get Contact from DB
   ├─► Get Template (or use default)
   ├─► Get TenantBranding
   │
   └─► Build data model:
       {
         Invoice: { Number, Date, Total, ... },
         Contact: { Name, Address, ... },
         Lines: [ { Desc, Qty, Price, ... } ],
         Branding: { Logo, Colors, ... }
       }
   │
   ▼
4. ITemplateRenderer (Scriban)
   - Parse HTML template
   - Inject data model
   - Return rendered HTML
   │
   ▼
5. IPdfRenderer (Playwright)
   - Launch headless browser
   - Load HTML + CSS
   - Generate PDF bytes
   │
   ▼
6. IFileStorage (LocalFileStorage)
   - Save PDF to ./storage/{tenant}/Invoices/
   - Create StoredFile record in DB
   - Update Invoice.PdfFileId
   │
   ▼
7. Response
   - Return PDF bytes to client
   - Client downloads file
```

## Entity Relationships

```
Tenant
  │
  ├─► InvoiceTemplate (1:N)
  │     - Name, IsDefault
  │     - HtmlTemplate, CssTemplate
  │
  ├─► TenantBranding (1:1)
  │     - Logo, Colors, Fonts, Footer
  │
  ├─► SalesInvoice (1:N)
  │     │
  │     ├─► Contact (N:1)
  │     │     - Customer info
  │     │
  │     ├─► InvoiceTemplate (N:1, optional)
  │     │     - Template used
  │     │
  │     ├─► StoredFile (N:1, optional)
  │     │     - Generated PDF
  │     │
  │     ├─► JournalEntry (N:1, optional)
  │     │     - Accounting entry
  │     │
  │     └─► SalesInvoiceLine (1:N)
  │           - Qty, Price, VAT
  │           - Account (N:1, optional)
  │
  └─► StoredFile (1:N)
        - PDFs, Logos, etc.
```

## Template Rendering Pipeline

```
Template Source
  │
  ├─► HTML Template (Scriban syntax)
  │   └─ "Invoice {{ Invoice.Number }}"
  │      "{{for line in Lines}}"
  │      "  {{ line.Description }}"
  │      "{{end}}"
  │
  └─► CSS Template
      └─ "h1 { color: {{ Branding.PrimaryColor }}; }"
         "body { font-family: {{ Branding.FontFamily }}; }"

         │
         ▼

Data Model
  {
    Invoice: { Number, Date, Total },
    Contact: { Name, Address },
    Lines: [ {Desc, Qty, Price} ],
    Branding: { Logo, Colors }
  }

         │
         ▼

Scriban Template Engine
  - Parse template
  - Replace {{ }} placeholders
  - Execute {{for}} loops
  - Evaluate {{if}} conditions

         │
         ▼

Rendered HTML
  <html>
    <style>h1{color:#0066cc}</style>
    <h1>Invoice INV-001</h1>
    <table>
      <tr><td>Item 1</td><td>10</td></tr>
      <tr><td>Item 2</td><td>5</td></tr>
    </table>
  </html>

         │
         ▼

Playwright PDF Renderer
  - Launch Chrome headless
  - Set content (HTML + CSS)
  - Print to PDF (A4, margins)

         │
         ▼

PDF Bytes
  [Binary PDF Data]

         │
         ▼

File Storage
  ./storage/{tenant}/Invoices/{guid}.pdf
  + StoredFile record in DB
```

## Multi-Tenancy Flow

```
HTTP Request
  └─► Header: X-Tenant-Id

         │
         ▼

TenantMiddleware
  - Extract tenant from header
  - Set ITenantContext.TenantId
  - Validate tenant access

         │
         ▼

DbContext SaveChanges
  - Auto-set TenantId on new entities
  - Apply query filters

         │
         ▼

Query Filters (EF Core)
  InvoiceTemplate.Where(t => t.TenantId == currentTenant)
  SalesInvoice.Where(i => i.TenantId == currentTenant)
  StoredFile.Where(f => f.TenantId == currentTenant)

         │
         ▼

File Storage
  ./storage/{tenant-id}/
    ├─ Invoices/
    └─ Logos/

  Complete tenant isolation at:
  - Database level (query filters)
  - File system level (folders)
```

## Status Workflow

```
┌─────────┐
│  DRAFT  │ Initial state
└────┬────┘
     │ Can edit/delete
     │ Can add/remove lines
     │ Can change customer
     │
     ▼
┌─────────┐
│  SENT   │ Sent to customer
└────┬────┘
     │ PDF generated
     │ Email sent (future)
     │
     ▼
┌─────────┐
│ POSTED  │ Booked to accounting
└────┬────┘
     │ Journal entry created
     │ Debit: Accounts Receivable
     │ Credit: Revenue + VAT
     │
     ▼
┌─────────┐
│  PAID   │ Payment received
└─────────┘
     │ Payment recorded (future)
     │ Linked to bank transaction
```

## File Organization

```
backend/
├─ src/
│  ├─ Domain/
│  │  ├─ Entities/
│  │  │  ├─ InvoiceTemplate.cs
│  │  │  ├─ TenantBranding.cs
│  │  │  ├─ SalesInvoice.cs
│  │  │  ├─ SalesInvoiceLine.cs
│  │  │  └─ StoredFile.cs
│  │  └─ Enums/
│  │     └─ InvoiceStatus.cs
│  │
│  ├─ Application/
│  │  ├─ DTOs/
│  │  │  ├─ InvoiceTemplates/
│  │  │  ├─ TenantBranding/
│  │  │  └─ SalesInvoices/
│  │  └─ Interfaces/
│  │     ├─ I*Service.cs (business logic)
│  │     ├─ ITemplateRenderer.cs
│  │     ├─ IPdfRenderer.cs
│  │     └─ IFileStorage.cs
│  │
│  ├─ Infrastructure/
│  │  ├─ Services/
│  │  │  ├─ *Service.cs (implementations)
│  │  │  ├─ ScribanTemplateRenderer.cs
│  │  │  ├─ PlaywrightPdfRenderer.cs
│  │  │  ├─ LocalFileStorage.cs
│  │  │  └─ DefaultTemplates.cs
│  │  └─ Migrations/
│  │     └─ *_AddSalesInvoicesAndTemplates.cs
│  │
│  └─ Api/
│     └─ Controllers/
│        ├─ InvoiceTemplatesController.cs
│        ├─ TenantBrandingController.cs
│        └─ SalesInvoicesController.cs
│
├─ storage/  (created at runtime)
│  └─ {tenant-id}/
│     ├─ Invoices/
│     └─ Logos/
│
└─ test-sales-invoices.ps1

frontend/
├─ src/
│  ├─ types/
│  │  └─ invoices.ts
│  └─ app/
│     └─ settings/
│        ├─ branding/
│        │  ├─ page.tsx
│        │  └─ branding.module.css
│        └─ templates/
│           ├─ page.tsx
│           └─ templates.module.css
```

## Technology Stack

```
┌─────────────────────────────────────┐
│          Frontend Layer              │
├─────────────────────────────────────┤
│  Next.js 14                          │
│  React 18                            │
│  TypeScript                          │
│  CSS Modules                         │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│          Backend Layer               │
├─────────────────────────────────────┤
│  .NET 8                              │
│  ASP.NET Core Web API                │
│  Entity Framework Core 8             │
│  PostgreSQL (Npgsql)                 │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│        Libraries & Packages          │
├─────────────────────────────────────┤
│  Scriban 5.10.0                      │
│    - Template rendering              │
│    - Liquid-like syntax              │
│                                      │
│  Microsoft.Playwright 1.48.0         │
│    - PDF generation                  │
│    - Browser automation              │
│                                      │
│  JWT Bearer Authentication           │
│    - User authentication             │
│    - Token validation                │
└─────────────────────────────────────┘
```

This architecture provides:
- ✅ Clean separation of concerns
- ✅ Swappable implementations (interfaces)
- ✅ Multi-tenant isolation
- ✅ Scalable file storage
- ✅ Type-safe APIs
- ✅ Professional PDF output
